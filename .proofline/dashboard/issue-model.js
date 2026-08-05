(function exposeProoflineIssueModel(root, factory) {
  const api = factory();

  if (typeof module === 'object' && module.exports) {
    module.exports = api;
  }

  if (root) {
    root.ProoflineIssueModel = api;
  }
}(typeof globalThis === 'object' ? globalThis : this, () => {
  'use strict';

  const issueTypes = new Set(['bug', 'task', 'feature', 'research', 'documentation', 'maintenance']);
  const issueModes = new Set(['simple', 'composite']);
  const issueStatuses = new Set(['open', 'doing', 'blocked', 'resolved', 'cancelled', 'superseded']);
  const activeStatuses = new Set(['open', 'doing', 'blocked']);
  const risks = new Set(['critical', 'high', 'medium', 'low']);
  const claimStates = new Set(['reported', 'confirmed', 'refuted']);
  const milestoneStatuses = new Set(['pending', 'doing', 'blocked', 'done']);
  const relationTypes = new Set(['child_of', 'superseded_by', 'follow_up_to']);
  const eventKinds = new Set(['decision', 'transition']);
  const terminalStatuses = new Set(['resolved', 'cancelled', 'superseded']);
  const requestedWorkTypes = new Set(['task', 'feature', 'documentation', 'maintenance']);

  const topLevelOrder = [
    'schema_version',
    'identity',
    'origin',
    'state',
    'problem',
    'objective',
    'criteria',
    'milestones',
    'relations',
    'context',
    'artifacts',
    'evidence',
    'events',
    'created_at',
    'updated_at'
  ];

  function isPlainObject(value) {
    return value !== null && typeof value === 'object' && !Array.isArray(value);
  }

  function isNonEmptyString(value) {
    return typeof value === 'string' && value.trim().length > 0;
  }

  function asArray(value) {
    return Array.isArray(value) ? value : [];
  }

  function addError(errors, condition, path, message) {
    if (!condition) {
      errors.push(`${path}: ${message}`);
    }
  }

  function addSoftBudgetWarning(warnings, value, limit, path) {
    if (typeof value === 'string' && value.length > limit) {
      warnings.push(`${path}: ${limit}자를 넘었습니다. 요약하거나 artifact/child issue 분리를 검토하세요.`);
    }
  }

  function rejectPlaceholders(value, path, errors) {
    if (typeof value === 'string') {
      addError(errors, !value.startsWith('REPLACE:') && !value.includes('{{'), path, '템플릿 값을 실제 값으로 교체해야 합니다.');
      return;
    }
    if (Array.isArray(value)) {
      value.forEach((item, index) => rejectPlaceholders(item, `${path}[${index}]`, errors));
      return;
    }
    if (isPlainObject(value)) {
      Object.entries(value).forEach(([key, item]) => rejectPlaceholders(item, path === '$' ? key : `${path}.${key}`, errors));
    }
  }

  function isTimestamp(value) {
    return isNonEmptyString(value) && !Number.isNaN(Date.parse(value));
  }

  function uniqueIds(items, pattern, path, errors) {
    const ids = new Set();

    for (const [index, item] of items.entries()) {
      const itemPath = `${path}[${index}]`;
      addError(errors, isPlainObject(item), itemPath, '객체여야 합니다.');
      if (!isPlainObject(item)) {
        continue;
      }

      addError(errors, isNonEmptyString(item.id), `${itemPath}.id`, '필수 문자열입니다.');
      if (!isNonEmptyString(item.id)) {
        continue;
      }

      addError(errors, pattern.test(item.id), `${itemPath}.id`, `형식이 올바르지 않습니다: ${item.id}`);
      addError(errors, !ids.has(item.id), `${itemPath}.id`, `중복 ID입니다: ${item.id}`);
      ids.add(item.id);
    }

    return ids;
  }

  function validateStringArray(value, path, errors) {
    addError(errors, Array.isArray(value), path, '배열이어야 합니다.');
    if (!Array.isArray(value)) {
      return;
    }

    value.forEach((item, index) => {
      addError(errors, isNonEmptyString(item), `${path}[${index}]`, '비어 있지 않은 문자열이어야 합니다.');
    });
  }

  function validateV2Issue(issue) {
    const errors = [];
    const warnings = [];

    addError(errors, isPlainObject(issue), '$', 'JSON 객체여야 합니다.');
    if (!isPlainObject(issue)) {
      return { valid: false, errors, warnings };
    }
    rejectPlaceholders(issue, '$', errors);

    addError(errors, issue.schema_version === 2, 'schema_version', '2여야 합니다.');
    addError(errors, isPlainObject(issue.identity), 'identity', '객체여야 합니다.');
    addError(errors, isPlainObject(issue.origin), 'origin', '객체여야 합니다.');
    addError(errors, isPlainObject(issue.state), 'state', '객체여야 합니다.');
    addError(errors, Array.isArray(issue.criteria), 'criteria', '배열이어야 합니다.');
    addError(errors, Array.isArray(issue.relations), 'relations', '배열이어야 합니다.');
    addError(errors, Array.isArray(issue.context), 'context', '배열이어야 합니다.');
    addError(errors, Array.isArray(issue.artifacts), 'artifacts', '배열이어야 합니다.');
    addError(errors, Array.isArray(issue.evidence), 'evidence', '배열이어야 합니다.');
    addError(errors, Array.isArray(issue.events), 'events', '배열이어야 합니다.');
    addError(errors, isNonEmptyString(issue.created_at), 'created_at', '필수 문자열입니다.');
    addError(errors, isNonEmptyString(issue.updated_at), 'updated_at', '필수 문자열입니다.');
    addError(errors, isTimestamp(issue.created_at), 'created_at', '파싱 가능한 날짜 또는 ISO 8601 시각이어야 합니다.');
    addError(errors, isTimestamp(issue.updated_at), 'updated_at', '파싱 가능한 날짜 또는 ISO 8601 시각이어야 합니다.');
    if (isTimestamp(issue.created_at) && isTimestamp(issue.updated_at)) {
      addError(errors, Date.parse(issue.updated_at) >= Date.parse(issue.created_at), 'updated_at', 'created_at보다 빠를 수 없습니다.');
    }

    const identity = isPlainObject(issue.identity) ? issue.identity : {};
    const origin = isPlainObject(issue.origin) ? issue.origin : {};
    const state = isPlainObject(issue.state) ? issue.state : {};
    const criteria = asArray(issue.criteria);
    const milestones = asArray(issue.milestones);
    const relations = asArray(issue.relations);
    const context = asArray(issue.context);
    const artifacts = asArray(issue.artifacts);
    const evidence = asArray(issue.evidence);
    const events = asArray(issue.events);

    addError(errors, /^PL-\d{4,}$/.test(identity.id || ''), 'identity.id', 'PL-0001 형식이어야 합니다.');
    addError(errors, Array.isArray(identity.aliases), 'identity.aliases', '배열이어야 합니다.');
    if (Array.isArray(identity.aliases)) {
      validateStringArray(identity.aliases, 'identity.aliases', errors);
    }
    addError(errors, issueTypes.has(identity.type), 'identity.type', '지원하는 이슈 유형이어야 합니다.');
    addError(errors, issueModes.has(identity.mode), 'identity.mode', 'simple 또는 composite여야 합니다.');
    addError(errors, isNonEmptyString(identity.title), 'identity.title', '필수 문자열입니다.');
    addError(errors, risks.has(identity.risk), 'identity.risk', '지원하는 위험도여야 합니다.');

    addError(errors, isNonEmptyString(origin.kind), 'origin.kind', '필수 문자열입니다.');
    addError(errors, isNonEmptyString(origin.summary), 'origin.summary', '필수 문자열입니다.');
    validateStringArray(origin.refs, 'origin.refs', errors);

    addError(errors, issueStatuses.has(state.status), 'state.status', '지원하는 상태여야 합니다.');
    addError(errors, isNonEmptyString(state.current_summary), 'state.current_summary', '필수 문자열입니다.');
    addSoftBudgetWarning(warnings, state.current_summary, 1200, 'state.current_summary');

    if (activeStatuses.has(state.status)) {
      addError(errors, isNonEmptyString(state.next_action), 'state.next_action', '활성 상태에서 필수입니다.');
    } else {
      addError(errors, !isNonEmptyString(state.next_action), 'state.next_action', '종료 상태에서는 사용할 수 없습니다.');
    }
    addSoftBudgetWarning(warnings, state.next_action, 600, 'state.next_action');

    if (state.status === 'blocked') {
      addError(errors, isNonEmptyString(state.blocker), 'state.blocker', 'blocked 상태에서 필수입니다.');
      addError(errors, isNonEmptyString(state.unblock_condition), 'state.unblock_condition', 'blocked 상태에서 필수입니다.');
    } else {
      addError(errors, !isNonEmptyString(state.blocker), 'state.blocker', 'blocked 상태에서만 사용할 수 있습니다.');
      addError(errors, !isNonEmptyString(state.unblock_condition), 'state.unblock_condition', 'blocked 상태에서만 사용할 수 있습니다.');
    }

    const usesClaims = identity.type === 'bug' || identity.type === 'research';
    if (usesClaims) {
      addError(errors, isPlainObject(issue.problem), 'problem', 'bug와 research에서 필수 객체입니다.');
      addError(errors, issue.objective === undefined, 'objective', 'bug와 research에서는 사용할 수 없습니다.');
    } else if (requestedWorkTypes.has(identity.type)) {
      addError(errors, isPlainObject(issue.objective), 'objective', '요청형 작업에서 필수 객체입니다.');
      addError(errors, issue.problem === undefined, 'problem', '요청형 작업에서는 사용할 수 없습니다.');
    }

    const claims = usesClaims && isPlainObject(issue.problem) ? asArray(issue.problem.claims) : [];
    if (usesClaims) {
      addError(errors, claims.length > 0, 'problem.claims', '하나 이상의 문제 주장 또는 연구 주장이 필요합니다.');
      addError(errors, isNonEmptyString(issue.problem.impact), 'problem.impact', '필수 문자열입니다.');
    } else if (isPlainObject(issue.objective)) {
      addError(errors, isNonEmptyString(issue.objective.summary), 'objective.summary', '필수 문자열입니다.');
      validateStringArray(issue.objective.constraints, 'objective.constraints', errors);
      addSoftBudgetWarning(warnings, issue.objective.summary, 800, 'objective.summary');
    }

    addError(errors, criteria.length > 0, 'criteria', '하나 이상의 완료 조건이 필요합니다.');
    const claimIds = uniqueIds(claims, /^P\d+$/, 'problem.claims', errors);
    const criterionIds = uniqueIds(criteria, /^C\d+$/, 'criteria', errors);
    const evidenceIds = uniqueIds(evidence, /^E\d+$/, 'evidence', errors);
    const milestoneIds = uniqueIds(milestones, /^M\d+$/, 'milestones', errors);
    const eventIds = uniqueIds(events, /^(D|T)\d+$/, 'events', errors);

    for (const [index, claim] of claims.entries()) {
      if (!isPlainObject(claim)) {
        continue;
      }
      addError(errors, claimStates.has(claim.state), `problem.claims[${index}].state`, 'reported, confirmed, refuted 중 하나여야 합니다.');
      addError(errors, isNonEmptyString(claim.text), `problem.claims[${index}].text`, '필수 문자열입니다.');
      validateStringArray(claim.evidence_refs, `problem.claims[${index}].evidence_refs`, errors);
      if (claim.state === 'confirmed' || claim.state === 'refuted') {
        addError(errors, asArray(claim.evidence_refs).length > 0, `problem.claims[${index}].evidence_refs`, `${claim.state} 주장에는 근거가 필요합니다.`);
      }
    }

    for (const [index, criterion] of criteria.entries()) {
      if (!isPlainObject(criterion)) {
        continue;
      }
      addError(errors, isNonEmptyString(criterion.text), `criteria[${index}].text`, '필수 문자열입니다.');
      validateStringArray(criterion.evidence_refs, `criteria[${index}].evidence_refs`, errors);
    }

    if (identity.mode === 'composite') {
      addError(errors, Array.isArray(issue.milestones), 'milestones', 'composite 이슈에서 필수 배열입니다.');
      addError(errors, milestones.length >= 3 && milestones.length <= 7, 'milestones', 'composite 이슈는 3~7개의 마일스톤을 가져야 합니다.');
    } else {
      addError(errors, milestones.length === 0, 'milestones', 'simple 이슈에서는 사용할 수 없습니다.');
    }

    for (const [index, milestone] of milestones.entries()) {
      if (!isPlainObject(milestone)) {
        continue;
      }
      addError(errors, isNonEmptyString(milestone.summary), `milestones[${index}].summary`, '필수 문자열입니다.');
      addError(errors, milestoneStatuses.has(milestone.status), `milestones[${index}].status`, '지원하는 마일스톤 상태여야 합니다.');
      validateStringArray(milestone.criteria_refs, `milestones[${index}].criteria_refs`, errors);
      validateStringArray(milestone.issue_refs, `milestones[${index}].issue_refs`, errors);
      asArray(milestone.criteria_refs).forEach((id) => {
        addError(errors, criterionIds.has(id), `milestones[${index}].criteria_refs`, `알 수 없는 완료 조건입니다: ${id}`);
      });
    }

    const relationKeys = new Set();
    let childOfCount = 0;
    let supersededByCount = 0;
    for (const [index, relation] of relations.entries()) {
      addError(errors, isPlainObject(relation), `relations[${index}]`, '객체여야 합니다.');
      if (!isPlainObject(relation)) {
        continue;
      }
      addError(errors, relationTypes.has(relation.type), `relations[${index}].type`, '지원하는 관계 유형이어야 합니다.');
      addError(errors, /^PL-\d{4,}$/.test(relation.target || ''), `relations[${index}].target`, '이슈 ID여야 합니다.');
      addError(errors, relation.target !== identity.id, `relations[${index}].target`, '자기 자신을 참조할 수 없습니다.');
      const relationKey = `${relation.type}:${relation.target}`;
      addError(errors, !relationKeys.has(relationKey), `relations[${index}]`, '중복 관계입니다.');
      relationKeys.add(relationKey);
      if (relation.type === 'child_of') {
        childOfCount += 1;
      }
      if (relation.type === 'superseded_by') {
        supersededByCount += 1;
      }
    }
    addError(errors, childOfCount <= 1, 'relations', 'child_of 관계는 하나만 가질 수 있습니다.');
    addError(errors, supersededByCount <= 1, 'relations', 'superseded_by 관계는 하나만 가질 수 있습니다.');

    for (const [index, item] of context.entries()) {
      addError(errors, isPlainObject(item), `context[${index}]`, '객체여야 합니다.');
      if (isPlainObject(item)) {
        addError(errors, isNonEmptyString(item.kind), `context[${index}].kind`, '필수 문자열입니다.');
        addError(errors, isNonEmptyString(item.location), `context[${index}].location`, '필수 문자열입니다.');
      }
    }

    for (const [index, artifact] of artifacts.entries()) {
      addError(errors, isPlainObject(artifact), `artifacts[${index}]`, '객체여야 합니다.');
      if (isPlainObject(artifact)) {
        addError(errors, isNonEmptyString(artifact.kind), `artifacts[${index}].kind`, '필수 문자열입니다.');
        addError(errors, isNonEmptyString(artifact.location), `artifacts[${index}].location`, '필수 문자열입니다.');
        addError(errors, isNonEmptyString(artifact.summary), `artifacts[${index}].summary`, '필수 문자열입니다.');
      }
    }

    const supersededEvidence = new Set();
    for (const [index, item] of evidence.entries()) {
      if (!isPlainObject(item)) {
        continue;
      }
      addError(errors, isNonEmptyString(item.kind), `evidence[${index}].kind`, '필수 문자열입니다.');
      addError(errors, isNonEmptyString(item.location), `evidence[${index}].location`, '필수 문자열입니다.');
      addError(errors, isNonEmptyString(item.observation), `evidence[${index}].observation`, '필수 문자열입니다.');
      addError(errors, isNonEmptyString(item.observed_at), `evidence[${index}].observed_at`, '필수 문자열입니다.');
      addError(errors, isTimestamp(item.observed_at), `evidence[${index}].observed_at`, '파싱 가능한 날짜 또는 ISO 8601 시각이어야 합니다.');
      addSoftBudgetWarning(warnings, item.observation, 800, `evidence[${index}].observation`);
      for (const field of ['supersedes', 'invalidates']) {
        if (item[field] !== undefined) {
          validateStringArray(item[field], `evidence[${index}].${field}`, errors);
          asArray(item[field]).forEach((id) => {
            addError(errors, evidenceIds.has(id), `evidence[${index}].${field}`, `알 수 없는 evidence입니다: ${id}`);
            supersededEvidence.add(id);
          });
        }
      }
    }

    const referencedEvidence = new Set();
    function validateEvidenceRefs(refs, path, requireActive = false) {
      for (const id of asArray(refs)) {
        addError(errors, evidenceIds.has(id), path, `알 수 없는 evidence입니다: ${id}`);
        if (evidenceIds.has(id)) {
          referencedEvidence.add(id);
        }
      }

      if (requireActive) {
        addError(
          errors,
          asArray(refs).some((id) => evidenceIds.has(id) && !supersededEvidence.has(id)),
          path,
          '현재 유효한 evidence가 하나 이상 필요합니다.'
        );
      }
    }

    claims.forEach((claim, index) => {
      if (isPlainObject(claim)) {
        validateEvidenceRefs(
          claim.evidence_refs,
          `problem.claims[${index}].evidence_refs`,
          claim.state === 'confirmed' || claim.state === 'refuted'
        );
      }
    });
    criteria.forEach((criterion, index) => {
      if (isPlainObject(criterion)) {
        validateEvidenceRefs(
          criterion.evidence_refs,
          `criteria[${index}].evidence_refs`,
          state.status === 'resolved'
        );
      }
    });

    const supersededDecisions = new Set();
    for (const [index, event] of events.entries()) {
      if (!isPlainObject(event)) {
        continue;
      }
      addError(errors, eventKinds.has(event.kind), `events[${index}].kind`, 'decision 또는 transition이어야 합니다.');
      addError(errors, isNonEmptyString(event.at), `events[${index}].at`, '필수 문자열입니다.');
      addError(errors, isTimestamp(event.at), `events[${index}].at`, '파싱 가능한 날짜 또는 ISO 8601 시각이어야 합니다.');
      addError(errors, isNonEmptyString(event.summary), `events[${index}].summary`, '필수 문자열입니다.');
      addSoftBudgetWarning(warnings, event.summary, 800, `events[${index}].summary`);

      if (event.kind === 'decision') {
        addError(errors, /^D\d+$/.test(event.id || ''), `events[${index}].id`, 'decision은 D1 형식이어야 합니다.');
        validateStringArray(event.evidence_refs, `events[${index}].evidence_refs`, errors);
        validateEvidenceRefs(event.evidence_refs, `events[${index}].evidence_refs`);
        if (event.supersedes !== undefined) {
          validateStringArray(event.supersedes, `events[${index}].supersedes`, errors);
          asArray(event.supersedes).forEach((id) => {
            addError(errors, eventIds.has(id) && /^D\d+$/.test(id), `events[${index}].supersedes`, `알 수 없는 decision입니다: ${id}`);
            supersededDecisions.add(id);
          });
        }
      }

      if (event.kind === 'transition') {
        addError(errors, /^T\d+$/.test(event.id || ''), `events[${index}].id`, 'transition은 T1 형식이어야 합니다.');
        addError(errors, issueStatuses.has(event.from), `events[${index}].from`, '지원하는 이전 상태여야 합니다.');
        addError(errors, issueStatuses.has(event.to), `events[${index}].to`, '지원하는 다음 상태여야 합니다.');
      }
    }

    for (const id of evidenceIds) {
      addError(errors, referencedEvidence.has(id), `evidence.${id}`, '어떤 주장·조건·결정에서도 참조하지 않는 고아 evidence입니다.');
    }

    if (state.status === 'resolved') {
      addError(errors, criteria.every((criterion) => asArray(criterion?.evidence_refs).length > 0), 'criteria', 'resolved 상태에서는 모든 완료 조건에 evidence가 필요합니다.');
      if (identity.mode === 'composite') {
        addError(
          errors,
          milestones.filter((milestone) => milestone?.required !== false).every((milestone) => milestone?.status === 'done'),
          'milestones',
          'resolved composite 이슈의 필수 마일스톤은 모두 done이어야 합니다.'
        );
      }
    }

    if (state.status === 'cancelled' || state.status === 'superseded') {
      addError(errors, events.some((event) => event?.kind === 'decision'), 'events', `${state.status} 상태에는 decision event가 필요합니다.`);
    }

    if (state.status === 'superseded') {
      addError(errors, relations.some((relation) => relation?.type === 'superseded_by'), 'relations', '대체 이슈를 가리키는 superseded_by 관계가 필요합니다.');
    }

    const transitions = events
      .filter((event) => event?.kind === 'transition' && isNonEmptyString(event.at))
      .sort((left, right) => String(left.at).localeCompare(String(right.at)));
    if (transitions.length > 0) {
      addError(errors, transitions.at(-1).to === state.status, 'events', '마지막 상태 전이와 현재 state.status가 일치해야 합니다.');
    }

    return {
      valid: errors.length === 0,
      errors,
      warnings,
      indexes: {
        claimIds,
        criterionIds,
        evidenceIds,
        milestoneIds,
        eventIds,
        supersededDecisions,
        supersededEvidence
      }
    };
  }

  function parseLegacyMarkdown(content, fileName) {
    const match = content.match(/^---\s*\n([\s\S]*?)\n---\s*\n?([\s\S]*)$/);
    if (!match) {
      throw new Error('JSON front matter가 없습니다.');
    }

    let metadata;
    try {
      metadata = JSON.parse(match[1]);
    } catch (error) {
      throw new Error(`JSON front matter 파싱 실패: ${error.message}`);
    }

    const requiredFields = ['id', 'status', 'title', 'evidence', 'risk', 'created_at', 'updated_at'];
    const missingFields = requiredFields.filter((field) => metadata[field] === undefined || metadata[field] === null);
    if (missingFields.length > 0) {
      throw new Error(`필수 필드 누락: ${missingFields.join(', ')}`);
    }

    const normalizedStatus = normalizeLegacyStatus(metadata.status);
    const logs = asArray(metadata.work_log);
    const latestLog = [...logs]
      .filter(isPlainObject)
      .sort((left, right) => String(left.at || '').localeCompare(String(right.at || '')))
      .at(-1);
    const resolvedLog = [...logs]
      .filter((item) => item?.status === 'resolved')
      .sort((left, right) => String(left.at || '').localeCompare(String(right.at || '')))
      .at(-1);
    const description = isNonEmptyString(metadata.description)
      ? metadata.description
      : extractLegacyDescription(match[2], metadata.title);
    const currentSummary = normalizedStatus === 'resolved'
      ? resolvedLog?.summary || description
      : latestLog?.summary || description;

    return {
      schemaVersion: 1,
      fileName,
      raw: metadata,
      id: String(metadata.id),
      aliases: [metadata.legacy_id].filter(isNonEmptyString),
      type: issueTypes.has(metadata.type) ? metadata.type : 'task',
      mode: 'legacy',
      status: normalizedStatus,
      title: String(metadata.title),
      risk: risks.has(metadata.risk) ? metadata.risk : 'medium',
      origin: {
        kind: 'legacy',
        summary: metadata.discovered_while || metadata.migration_source || '레거시 이슈',
        refs: []
      },
      currentSummary,
      nextAction: activeStatuses.has(normalizedStatus) ? metadata.suggested_next_step || '' : '',
      blocker: '',
      unblockCondition: '',
      subjectKind: metadata.type === 'bug' ? 'problem' : 'objective',
      claims: metadata.type === 'bug' && description
        ? [{ id: 'P1', state: 'reported', text: description, evidenceRefs: [] }]
        : [],
      objective: metadata.type === 'bug' ? null : { summary: description, constraints: [] },
      impact: metadata.impact || '',
      criteria: normalizeLegacyCriteria(metadata.completion_criteria),
      milestones: [],
      relations: normalizeLegacyRelations(metadata.linked_context),
      context: normalizeLegacyContext(metadata.linked_context),
      artifacts: [],
      evidence: normalizeLegacyEvidence(metadata.evidence, metadata.resolved_evidence),
      events: normalizeLegacyEvents(logs),
      effectiveDecisions: [],
      verification: normalizeLegacyVerification(metadata.resolved_evidence),
      legacyBody: match[2] || '',
      createdAt: metadata.created_at,
      updatedAt: metadata.updated_at,
      searchText: '',
      validation: { valid: true, errors: [], warnings: ['레거시 Markdown 이슈입니다.'] }
    };
  }

  function extractLegacyDescription(body, fallback) {
    const lines = String(body || '').split(/\r?\n/);
    let insideDescription = false;

    for (const line of lines) {
      if (/^##\s+(설명|내용|Description)\s*$/.test(line.trim())) {
        insideDescription = true;
        continue;
      }
      if (insideDescription && /^##\s+/.test(line)) {
        break;
      }
      if (insideDescription && line.trim() && !line.trim().startsWith('-')) {
        return line.trim();
      }
    }

    return String(fallback || '레거시 이슈');
  }

  function normalizeLegacyStatus(status) {
    if (status === 'in_progress') {
      return 'doing';
    }
    if (status === 'ignored') {
      return 'cancelled';
    }
    return issueStatuses.has(status) ? status : 'open';
  }

  function normalizeLegacyCriteria(value) {
    return asArray(value).map((item, index) => ({
      id: `C${index + 1}`,
      text: typeof item === 'string' ? item : item?.text || item?.summary || JSON.stringify(item),
      evidenceRefs: []
    }));
  }

  function normalizeLegacyEvidence(evidence, resolvedEvidence) {
    return [...asArray(evidence), ...asArray(resolvedEvidence)].map((item, index) => {
      if (typeof item === 'string') {
        return { id: `E${index + 1}`, kind: 'legacy', location: 'legacy issue', observation: item };
      }
      return {
        id: `E${index + 1}`,
        kind: item?.kind || 'legacy',
        location: item?.location || 'legacy issue',
        observation: item?.observation || item?.note || JSON.stringify(item)
      };
    });
  }

  function normalizeLegacyVerification(value) {
    return asArray(value).map((item) => typeof item === 'string'
      ? item
      : item?.observation || item?.note || JSON.stringify(item));
  }

  function normalizeLegacyContext(value) {
    if (Array.isArray(value)) {
      return value.map((location) => ({ kind: 'reference', location: String(location) }));
    }
    if (!isPlainObject(value)) {
      return [];
    }

    const context = [];
    for (const [key, entry] of Object.entries(value)) {
      if (Array.isArray(entry)) {
        entry.forEach((location) => context.push({ kind: key.replace(/s$/, '') || 'reference', location: String(location) }));
      } else if (isNonEmptyString(entry)) {
        context.push({ kind: key, location: entry });
      }
    }
    return context;
  }

  function normalizeLegacyRelations(value) {
    if (!isPlainObject(value) || !Array.isArray(value.issues)) {
      return [];
    }
    return value.issues.map((target) => ({ type: 'related', target: String(target) }));
  }

  function normalizeLegacyEvents(logs) {
    return asArray(logs).map((item, index) => ({
      id: `L${index + 1}`,
      kind: 'legacy',
      at: item?.at || '',
      summary: item?.summary || JSON.stringify(item),
      status: normalizeLegacyStatus(item?.status)
    }));
  }

  function parseV2Json(content, fileName) {
    let issue;
    try {
      issue = JSON.parse(String(content).replace(/^\uFEFF/, ''));
    } catch (error) {
      throw new Error(`JSON 파싱 실패: ${error.message}`);
    }

    const validation = validateV2Issue(issue);
    if (!validation.valid) {
      throw new Error(validation.errors.join('; '));
    }

    return toViewModel(issue, fileName, validation);
  }

  function parseIssueContent(content, fileName) {
    if (typeof fileName !== 'string') {
      throw new Error('파일 이름이 필요합니다.');
    }
    if (fileName.endsWith('.json')) {
      return parseV2Json(content, fileName);
    }
    if (fileName.endsWith('.md')) {
      const issue = parseLegacyMarkdown(content, fileName);
      issue.searchText = buildSearchText(issue);
      return issue;
    }
    throw new Error('지원하지 않는 이슈 파일 형식입니다.');
  }

  function toViewModel(issue, fileName, validation = validateV2Issue(issue)) {
    const identity = issue.identity;
    const state = issue.state;
    const usesClaims = identity.type === 'bug' || identity.type === 'research';
    const invalidated = validation.indexes?.supersededEvidence || new Set();
    const evidence = issue.evidence.map((item) => ({
      id: item.id,
      kind: item.kind,
      location: item.location,
      observation: item.observation,
      observedAt: item.observed_at,
      active: !invalidated.has(item.id),
      supersedes: asArray(item.supersedes),
      invalidates: asArray(item.invalidates)
    }));
    const supersededDecisions = validation.indexes?.supersededDecisions || new Set();
    const events = issue.events.map((event) => ({
      id: event.id,
      kind: event.kind,
      at: event.at,
      summary: event.summary,
      from: event.from,
      to: event.to,
      evidenceRefs: asArray(event.evidence_refs),
      supersedes: asArray(event.supersedes),
      effective: event.kind !== 'decision' || !supersededDecisions.has(event.id)
    }));

    const view = {
      schemaVersion: 2,
      fileName,
      raw: issue,
      id: identity.id,
      aliases: asArray(identity.aliases),
      type: identity.type,
      mode: identity.mode,
      status: state.status,
      title: identity.title,
      risk: identity.risk,
      origin: issue.origin,
      currentSummary: state.current_summary,
      nextAction: state.next_action || '',
      blocker: state.blocker || '',
      unblockCondition: state.unblock_condition || '',
      subjectKind: usesClaims ? 'problem' : 'objective',
      claims: usesClaims ? issue.problem.claims.map((claim) => ({
        id: claim.id,
        state: claim.state,
        text: claim.text,
        evidenceRefs: asArray(claim.evidence_refs)
      })) : [],
      objective: usesClaims ? null : issue.objective,
      impact: usesClaims ? issue.problem.impact : '',
      criteria: issue.criteria.map((criterion) => ({
        id: criterion.id,
        text: criterion.text,
        evidenceRefs: asArray(criterion.evidence_refs)
      })),
      milestones: asArray(issue.milestones).map((milestone) => ({
        id: milestone.id,
        summary: milestone.summary,
        status: milestone.status,
        required: milestone.required !== false,
        criteriaRefs: asArray(milestone.criteria_refs),
        issueRefs: asArray(milestone.issue_refs)
      })),
      relations: issue.relations,
      context: issue.context,
      artifacts: issue.artifacts,
      evidence,
      events,
      effectiveDecisions: events.filter((event) => event.kind === 'decision' && event.effective),
      verification: [],
      legacyBody: '',
      createdAt: issue.created_at,
      updatedAt: issue.updated_at,
      validation,
      searchText: ''
    };
    view.searchText = buildSearchText(view);
    return view;
  }

  function isIssueFileName(fileName) {
    return typeof fileName === 'string'
      && (fileName.endsWith('.json') || fileName.endsWith('.md'))
      && !fileName.endsWith('.example.json')
      && !fileName.endsWith('.example.md')
      && !fileName.includes('/')
      && !fileName.includes('\\');
  }

  function collectText(value, target) {
    if (value === undefined || value === null) {
      return;
    }
    if (typeof value === 'string' || typeof value === 'number' || typeof value === 'boolean') {
      target.push(String(value));
      return;
    }
    if (Array.isArray(value)) {
      value.forEach((item) => collectText(item, target));
      return;
    }
    if (isPlainObject(value)) {
      Object.values(value).forEach((item) => collectText(item, target));
    }
  }

  function buildSearchText(issue) {
    const text = [];
    collectText(issue.schemaVersion === 2 ? issue.raw : issue, text);
    return text.join(' ').toLowerCase();
  }

  function buildBrief(issue) {
    const view = issue.schemaVersion ? issue : toViewModel(issue, issue.identity?.id ? `${issue.identity.id}.json` : 'issue.json');
    const lines = [
      `[${view.id}] ${view.status} ${view.type}/${view.mode} | ${view.title}`,
      `summary: ${view.currentSummary}`
    ];

    if (view.nextAction) {
      lines.push(`next: ${view.nextAction}`);
    }
    if (view.blocker) {
      lines.push(`blocker: ${view.blocker}`);
      lines.push(`unblock: ${view.unblockCondition}`);
    }

    if (view.subjectKind === 'problem') {
      lines.push('claims:');
      view.claims.forEach((claim) => lines.push(`- ${claim.id} ${claim.state}${formatEvidenceIndex(claim.evidenceRefs)}: ${claim.text}`));
      if (view.impact) {
        lines.push(`impact: ${view.impact}`);
      }
    } else if (view.objective) {
      lines.push(`objective: ${view.objective.summary}`);
      if (asArray(view.objective.constraints).length) {
        lines.push('constraints:');
        view.objective.constraints.forEach((constraint) => lines.push(`- ${constraint}`));
      }
    }

    lines.push('criteria:');
    view.criteria.forEach((criterion) => lines.push(`- ${criterion.id}${formatEvidenceIndex(criterion.evidenceRefs)}: ${criterion.text}`));

    if (view.milestones.length) {
      lines.push('milestones:');
      view.milestones.forEach((milestone) => lines.push(`- ${milestone.id} ${milestone.status}: ${milestone.summary}`));
    }

    if (view.effectiveDecisions.length) {
      lines.push('decisions:');
      view.effectiveDecisions.forEach((decision) => lines.push(`- ${decision.id}${formatEvidenceIndex(decision.evidenceRefs)}: ${decision.summary}`));
    }

    if (view.relations.length) {
      lines.push('relations:');
      view.relations.forEach((relation) => lines.push(`- ${relation.type}: ${relation.target}`));
    }

    return `${lines.join('\n')}\n`;
  }

  function formatEvidenceIndex(evidenceRefs) {
    return asArray(evidenceRefs).length ? ` [${evidenceRefs.join(',')}]` : '';
  }

  function orderIssue(issue) {
    const ordered = {};
    for (const key of topLevelOrder) {
      if (issue[key] !== undefined) {
        ordered[key] = issue[key];
      }
    }
    for (const [key, value] of Object.entries(issue)) {
      if (!(key in ordered)) {
        ordered[key] = value;
      }
    }
    return ordered;
  }

  function serializeIssue(issue) {
    return `${JSON.stringify(orderIssue(issue), null, 2)}\n`;
  }

  function cloneIssue(issue) {
    return JSON.parse(JSON.stringify(issue));
  }

  function nextLocalId(items, prefix) {
    const highest = asArray(items).reduce((maximum, item) => {
      const match = new RegExp(`^${prefix}(\\d+)$`).exec(item?.id || '');
      return match ? Math.max(maximum, Number(match[1])) : maximum;
    }, 0);
    return `${prefix}${highest + 1}`;
  }

  function applyMutation(issue, operation, operationAt) {
    if (operation.type === 'set_state') {
      const previousStatus = issue.state.status;
      issue.state.status = operation.status;
      for (const field of ['next_action', 'blocker', 'unblock_condition']) {
        if (operation[field] === null || operation[field] === undefined || operation[field] === '') {
          delete issue.state[field];
        } else {
          issue.state[field] = operation[field];
        }
      }
      if (previousStatus !== operation.status) {
        issue.events.push({
          id: nextLocalId(issue.events, 'T'),
          kind: 'transition',
          at: operationAt,
          from: previousStatus,
          to: operation.status,
          summary: operation.transition_summary || operation.current_summary
        });
      }
    } else if (operation.type === 'add_evidence') {
      issue.evidence.push(operation.evidence);
      const targets = asArray(operation.targets);
      if (targets.length === 0) {
        throw new Error('add_evidence에는 최소 하나의 P/C/D target이 필요합니다.');
      }
      targets.forEach((target) => linkEvidence(issue, target, operation.evidence?.id));
    } else if (operation.type === 'link_evidence') {
      linkEvidence(issue, operation.target, operation.evidence_id);
    } else if (operation.type === 'add_event') {
      issue.events.push(operation.event);
    } else if (operation.type === 'set_milestone') {
      const milestone = asArray(issue.milestones).find((item) => item.id === operation.milestone_id);
      if (!milestone) {
        throw new Error(`마일스톤을 찾지 못했습니다: ${operation.milestone_id}`);
      }
      milestone.status = operation.status;
    } else if (operation.type === 'add_relation') {
      issue.relations.push(operation.relation);
    } else {
      throw new Error(`지원하지 않는 operation입니다: ${operation.type}`);
    }
  }

  function applyOperation(issue, operation) {
    if (!isPlainObject(issue) || issue.schema_version !== 2) {
      throw new Error('v2 JSON 이슈만 구조화 갱신할 수 있습니다.');
    }
    if (!isPlainObject(operation) || !isNonEmptyString(operation.type)) {
      throw new Error('operation.type이 필요합니다.');
    }

    const operations = operation.type === 'batch' ? operation.operations : [operation];
    if (!Array.isArray(operations) || operations.length === 0) {
      throw new Error('batch.operations에는 하나 이상의 operation이 필요합니다.');
    }
    if (operations.some((item) => !isPlainObject(item) || !isNonEmptyString(item.type) || item.type === 'batch')) {
      throw new Error('batch에는 중첩되지 않은 유효한 operation만 사용할 수 있습니다.');
    }
    if (!isNonEmptyString(operation.current_summary)) {
      throw new Error(`${operation.type}에는 current_summary 재작성 또는 재확인이 필요합니다.`);
    }
    const next = cloneIssue(issue);
    const operationAt = operation.updated_at || new Date().toISOString();
    next.state.current_summary = operation.current_summary;
    operations.forEach((item) => applyMutation(
      next,
      { ...item, current_summary: operation.current_summary },
      operationAt
    ));
    next.updated_at = operationAt;
    const validation = validateV2Issue(next);
    if (!validation.valid) {
      throw new Error(validation.errors.join('; '));
    }
    return { issue: next, validation };
  }

  function linkEvidence(issue, target, evidenceId) {
    const candidates = [
      ...asArray(issue.problem?.claims),
      ...asArray(issue.criteria),
      ...asArray(issue.events).filter((event) => event.kind === 'decision')
    ];
    const record = candidates.find((item) => item.id === target);
    if (!record) {
      throw new Error(`evidence 연결 대상을 찾지 못했습니다: ${target}`);
    }
    if (!asArray(issue.evidence).some((item) => item.id === evidenceId)) {
      throw new Error(`evidence를 찾지 못했습니다: ${evidenceId}`);
    }
    record.evidence_refs = [...new Set([...asArray(record.evidence_refs), evidenceId])];
  }

  return {
    activeStatuses,
    terminalStatuses,
    issueStatuses,
    isIssueFileName,
    parseIssueContent,
    parseLegacyMarkdown,
    parseV2Json,
    validateV2Issue,
    toViewModel,
    buildSearchText,
    buildBrief,
    serializeIssue,
    applyOperation,
    normalizeLegacyStatus
  };
}));
