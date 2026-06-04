export const meta = {
  name: 'issue-workflow',
  description: 'Find ready GitHub issues, create branches, ask questions, implement, create PRs',
  whenToUse: 'Use when asked to find ready issues, check for answers, or implement an issue',
  phases: [
    { title: 'Find Issues' },
    { title: 'Branch & Questions' },
    { title: 'Implement' },
    { title: 'Pull Request' },
  ]
}

// Usage:
//   "find ready issues"       → Workflow({ name: 'issue-workflow', args: { mode: 'find-ready' } })
//   "check answers on #N"     → Workflow({ name: 'issue-workflow', args: { mode: 'check-answers', issue: N } })
//   "implement issue #N"      → Workflow({ name: 'issue-workflow', args: { mode: 'implement', issue: N } })

const REPO_SCHEMA = {
  type: 'object',
  properties: { nameWithOwner: { type: 'string' }, url: { type: 'string' } },
  required: ['nameWithOwner']
}
const ISSUES_SCHEMA = {
  type: 'object',
  properties: {
    issues: {
      type: 'array',
      items: {
        type: 'object',
        properties: {
          number: { type: 'number' },
          title:  { type: 'string' },
          body:   { type: 'string' },
          labels: { type: 'array', items: { type: 'string' } }
        },
        required: ['number', 'title', 'body']
      }
    }
  },
  required: ['issues']
}

const COMMENTS_SCHEMA = {
  type: 'object',
  properties: {
    number: { type: 'number' },
    title:  { type: 'string' },
    body:   { type: 'string' },
    comments: {
      type: 'array',
      items: {
        type: 'object',
        properties: {
          author: { type: 'object', properties: { login: { type: 'string' } } },
          authorAssociation: { type: 'string' },
          body: { type: 'string' }
        }
      }
    }
  },
  required: ['number', 'title', 'comments']
}

const mode        = args?.mode ?? 'find-ready'
const issueNumber = args?.issue
const PROJECT     = '/Users/palmer/Documents/Claude/Projects/Timberborn - Derivative Faction'

// Resolve repo
const repoInfo = await agent(
  'Run: gh repo view --json nameWithOwner,url  Return the JSON.',
  { schema: REPO_SCHEMA, label: 'repo-info' }
)
const REPO = repoInfo.nameWithOwner
log(`Repo: ${REPO}`)

// ── FIND READY ────────────────────────────────────────────────────────────────
if (mode === 'find-ready') {
  phase('Find Issues')
  const result = await agent(
    `List all open GitHub issues in ${REPO} that have the label "ready".
     Run: gh issue list --repo ${REPO} --label ready --json number,title,body,labels
     Return the parsed list.`,
    { schema: ISSUES_SCHEMA, label: 'find-ready-issues' }
  )

  const readyCount = result.issues.length
  if (!readyCount) {
    log('No issues labeled "ready" found.')
  } else {
    log(`Found ${readyCount} ready issue(s)`)
  }

  phase('Branch & Questions')
  const processed = readyCount ? await parallel(result.issues.map(issue => async () => {
    const slug   = issue.title.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '').slice(0, 40)
    const branch = `issue/${issue.number}-${slug}`

    return await agent(`
      Process GitHub issue #${issue.number}: "${issue.title}"

      Issue body:
      ${issue.body}

      Steps:
      1. cd "${PROJECT}" && git checkout -b ${branch} && git push -u origin ${branch}
      2. Analyse the issue. If anything is ambiguous, post a comment listing your questions
         (numbered list). Use:
           gh issue comment ${issue.number} --repo ${REPO} --body "..."
         If nothing is ambiguous, post a comment saying you are ready to implement.
      3. gh issue edit ${issue.number} --repo ${REPO} --add-label "in-progress" --remove-label "ready"

      Report: branch created, questions posted (or "no questions"), labels updated.
    `, { label: `process-${issue.number}` })
  })) : []

  // ── Scan in-progress issues for owner replies to bot questions ──────────────
  log('Scanning in-progress issues for answered questions...')
  const inProgressResult = await agent(
    `List all open GitHub issues in ${REPO} that have the label "in-progress".
     Run: gh issue list --repo ${REPO} --label in-progress --json number,title,body,labels
     Return the parsed list.`,
    { schema: ISSUES_SCHEMA, label: 'find-in-progress-issues' }
  )

  const answeredIssues = []
  if (inProgressResult.issues.length) {
    const commentResults = await parallel(inProgressResult.issues.map(issue => async () => {
      const data = await agent(
        `Fetch issue #${issue.number} and its comments from ${REPO}.
         Run: gh issue view ${issue.number} --repo ${REPO} --comments --json number,title,body,comments
         Return the parsed JSON.`,
        { schema: COMMENTS_SCHEMA, label: `comments-${issue.number}` }
      )
      return { issue, data }
    }))

    for (const { issue, data } of commentResults.filter(Boolean)) {
      const comments = data.comments ?? []
      // Detect: at least one non-owner comment (bot/Claude) followed by at least one OWNER comment
      let sawBotComment = false
      let ownerRepliedAfterBot = false
      for (const c of comments) {
        const assoc = (c.authorAssociation ?? '').toUpperCase()
        if (assoc !== 'OWNER') {
          sawBotComment = true
        } else if (sawBotComment && assoc === 'OWNER') {
          ownerRepliedAfterBot = true
          break
        }
      }
      if (ownerRepliedAfterBot) {
        log(`Issue #${issue.number} has owner replies to bot questions — queuing for implementation`)
        answeredIssues.push({ issue, data })
      }
    }
  }

  if (answeredIssues.length) {
    log(`Found ${answeredIssues.length} answered in-progress issue(s) — implementing now`)
    phase('Implement')
    await parallel(answeredIssues.map(({ issue, data }) => async () => {
      const issueNum = issue.number
      const issueTitle = issue.title
      const commentsText = (data.comments ?? []).map(c => `[${c.authorAssociation}] ${c.body}`).join('\n\n')

      await agent(`
        Implement the changes for issue #${issueNum}: "${issueTitle}"

        Original issue body:
        ${data.body}

        Comments (including owner answers to questions):
        ${commentsText}

        Project directory: ${PROJECT}

        Steps:
        1. cd "${PROJECT}" && git fetch origin
        2. Check out the existing branch: git checkout $(git branch -a | grep -o 'issue/${issueNum}-[^[:space:]]*' | head -1 | sed 's|remotes/origin/||')
           If no local branch matches, use: git checkout --track origin/$(git branch -r | grep -o 'issue/${issueNum}-[^[:space:]]*' | head -1)
        3. Implement all required changes based on the issue and answers.
        4. python3 validate_mod.py
        5. git add -A && git commit -m "Implement #${issueNum}: ${issueTitle}" && git push
        Report: branch name used, changes made, validation result, commit hash.
      `, { label: `implement-answered-${issueNum}` })
    }))

    phase('Pull Request')
    await parallel(answeredIssues.map(({ issue }) => async () => {
      const issueNum = issue.number
      await agent(`
        Create a PR for issue #${issueNum} in ${REPO} if one does not already exist.

        1. Check: gh pr list --repo ${REPO} --head $(git -C "${PROJECT}" branch -a | grep -o 'issue/${issueNum}-[^[:space:]]*' | head -1 | sed 's|remotes/origin/||') --json number --jq '.[0].number'
           If a PR number is returned, skip creation and just report the existing PR URL.
        2. If no PR exists:
           gh pr create --repo ${REPO} \
             --title "Fix #${issueNum}: ${issue.title}" \
             --body "## Summary
        [Summarise the implementation based on the issue and changes made]

        Closes #${issueNum}

        ## Test plan
        - [ ] python3 validate_mod.py passes
        - [ ] Mod builds in Unity without errors
        - [ ] Tested in-game

        ## Pre-merge checklist
        - [ ] Reviewer approved

        🤖 Generated with [Claude Code](https://claude.ai/claude-code)"
        3. Report the PR URL.
      `, { label: `pr-answered-${issueNum}` })
    }))
  } else {
    log('No in-progress issues with owner replies found.')
  }

  return { processed: processed.filter(Boolean).length, answeredAndImplemented: answeredIssues.length }
}

// ── CHECK ANSWERS ─────────────────────────────────────────────────────────────
if (mode === 'check-answers') {
  if (!issueNumber) throw new Error('args.issue required for check-answers mode')

  phase('Find Issues')
  const issueData = await agent(`
    Fetch issue #${issueNumber} and all its comments from ${REPO}.
    Run: gh issue view ${issueNumber} --repo ${REPO} --comments --json title,body,comments
    Summarise: original body + any answers from the issue author (not bots).
  `, { label: 'fetch-comments' })
  log(issueData)

  phase('Implement')
  await agent(`
    Based on issue #${issueNumber} and the answers:
    ${issueData}

    1. cd "${PROJECT}" && git checkout $(git branch -a | grep "issue/${issueNumber}" | head -1 | sed 's|.*origin/||')
    2. Implement the required changes.
    3. python3 validate_mod.py
    4. git add -A && git commit -m "Implement #${issueNumber}: ..." && git push
    Report: changes made, validation result, commit hash.
  `, { label: 'implement' })

  phase('Pull Request')
  await agent(`
    Create a PR for issue #${issueNumber} in ${REPO} if one does not already exist.
    Check: gh pr list --repo ${REPO} | grep "issue/${issueNumber}"
    If none:
      gh pr create --repo ${REPO} \
        --title "..." \
        --body "## Summary
    [summarise implementation]

    Closes #${issueNumber}

    ## Test plan
    - [ ] python3 validate_mod.py passes
    - [ ] Mod builds in Unity without errors
    - [ ] Tested in-game

    ## Pre-merge checklist
    - [ ] Reviewer approved

    🤖 Generated with [Claude Code](https://claude.ai/claude-code)"

    Report the PR URL.
  `, { label: 'create-pr' })
}

// ── IMPLEMENT (no prior question round needed) ────────────────────────────────
if (mode === 'implement') {
  if (!issueNumber) throw new Error('args.issue required for implement mode')

  phase('Find Issues')
  const issueData = await agent(`
    Fetch full details of issue #${issueNumber} from ${REPO} including comments.
    Run: gh issue view ${issueNumber} --repo ${REPO} --comments --json title,body,comments
    Return full content.
  `, { label: 'fetch-issue' })

  phase('Implement')
  await agent(`
    Implement issue #${issueNumber}:
    ${issueData}

    Project: ${PROJECT}
    1. git checkout issue/${issueNumber}-*
    2. Make all changes.
    3. python3 validate_mod.py
    4. git add -A && git commit -m "Implement #${issueNumber}: ..." && git push
    Report changes, validation result, commit hash.
  `, { label: 'implement-issue' })

  phase('Pull Request')
  await agent(`
    Create a PR for issue #${issueNumber} in ${REPO} if one doesn't exist.
    gh pr list --repo ${REPO} | grep "issue/${issueNumber}" || \
    gh pr create --repo ${REPO} --title "..." --body "Closes #${issueNumber} ..."
    Report PR URL.
  `, { label: 'pr' })
}
