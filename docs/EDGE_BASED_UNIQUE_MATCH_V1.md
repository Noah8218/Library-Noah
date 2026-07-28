# Edge-Based Unique Match V1

Status: implemented and bounded by synthetic smoke evidence

## Purpose

`EdgeBasedTemplateMatchingTool` can optionally reject ambiguous one-result
matches instead of returning only the best item and hiding a plausible second
location.

## API Contract

`IOpenCVPropertyEdgeBasedTemplateMatching` adds:

```csharp
bool USE_UNIQUE_MATCH_VALIDATION { get; set; }
double UNIQUE_MATCH_MIN_SCORE_MARGIN { get; set; }
```

The option is backward-compatible and disabled by default. The default normalized
margin is `0.03`. Enabled mode requires:

- `NUM_MATCH == 1`;
- `USE_MULTI_ROI == false`;
- a finite margin in `0..1`.

The internal candidate pool is at least Top 8 and is independent of the external
one-result count.

## Decision Contract

A candidate below the existing `SCORE_MIN` is `NoMatch`. An eligible alternative
must also meet `SCORE_MIN` and have a center at least
`max(8 px, 0.35 * min(template width, template height))` from the selected
candidate. It is plausible only when its selected-minus-alternative score margin
is below `UNIQUE_MATCH_MIN_SCORE_MARGIN`.

- no selected candidate: `MatchingNoResult`, zero results;
- accepted unique candidate: success, exactly one result;
- alternative inside the failed score-margin gate: `MatchingAmbiguous`, zero
  results.

Hybrid mode uses the finite hybrid selection score; otherwise the existing edge
score is used. The ambiguity message includes the best score, strongest
alternative score, actual and required margin, plausible-alternative count, and
matching options.

## Evidence

`VisionToolResult.Metrics` publishes:

- `UniqueMatch.Enabled`
- `UniqueMatch.State` (`0 Disabled`, `1 NoMatch`, `2 Success`, `3 Ambiguous`)
- `UniqueMatch.MinimumInternalTopK`
- `UniqueMatch.PlausibleAlternativeCount`
- `UniqueMatch.SelectedScore`
- `UniqueMatch.StrongestAlternativeScore`
- `UniqueMatch.ScoreMargin`
- `UniqueMatch.MinimumScoreMargin`
- `UniqueMatch.DistanceThresholdPx`

Metrics use normalized scores. Successful `MatchingResult` rows additionally
retain `EdgeScore`, optional `ImageScore`, `FinalScore`, and `ScoreMargin` in the
existing percentage-point presentation. Legacy-disabled rows keep
`ScoreMargin=NaN`.

The smoke matrix covers:

1. legacy repeated-pattern success;
2. unique distinct-pattern success;
3. repeated-pattern `MatchingAmbiguous`;
4. absent-pattern `MatchingNoResult`.

Set `LIB_NOAH_UNIQUE_MATCH_EVIDENCE_DIR` to retain sources, result drawings, and
summary text for those four cases.

## Boundary

This proves only the state/error/result/metric contract on deterministic
synthetic cases. It does not qualify a production template, search ROI, default
margin, pose precision, false-accept rate, repeatability, or field robustness.
