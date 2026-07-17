# UNIT-FEATURE-FRIENDLY-BATTLE-001: Friendly Battle Unit Plan

## Status

VERIFIED

## Requirement

FEATURE-FRIENDLY-BATTLE-001

## Purpose

Define unit-level evidence for accepted-friend authorization, friendly battle invite lifecycle,
room creation orchestration, ownership checks, and no-reward mutation.

## Test Cases

| Area | Case | Expected Result |
|---|---|---|
| Authorization | Accepted friends create invite | One pending invite is created. |
| Authorization | Non-friend invite | Invite is rejected with `FriendlyBattleNotFriends`. |
| Authorization | Self invite | Invite is rejected with `FriendlyBattleSelfInvite`. |
| Authorization | Pending or rejected friendship | Invite is rejected before persistence. |
| Duplicate | Existing pending invite | No duplicate pending invite is created. |
| Cancel | Challenger cancels | Invite status becomes `Cancelled`. |
| Cancel | Recipient cancels | Request is rejected with `FriendlyBattleForbidden`. |
| Decline | Recipient declines | Invite status becomes `Rejected`. |
| Decline | Challenger declines | Request is rejected with `FriendlyBattleForbidden`. |
| Accept | Recipient accepts | Invite status becomes `Accepted` and one room id is attached. |
| Accept | Challenger accepts | Request is rejected with `FriendlyBattleForbidden`. |
| Accept | Cancelled invite | Request is rejected with `FriendlyBattleAlreadyResolved`. |
| Accept | Declined invite | Request is rejected with `FriendlyBattleAlreadyResolved`. |
| Accept | Expired invite | Request is rejected with `FriendlyBattleInviteExpired`. |
| Battle | Room creation | Reused online battle snapshot contains both friends and starter deck. |
| Battle | Player already in battle | Invite accept/create is rejected with `PlayerAlreadyInBattle`. |
| Security | Non-participant room access | Request is rejected with `OnlineBattleForbidden`. |
| Rewards | Battle result | Trophies, gold, friendship status, and account type are unchanged. |
| Projection | Current snapshot | Incoming, outgoing, and active friendly room are returned for the actor. |

## Execution

Run:

```powershell
dotnet test CRPrivateServer.sln
```

## Evidence

GamePM, Dev, Asset, and QA verified this unit plan on 2026-07-18. `dotnet test
CRPrivateServer.sln` passed with Domain 31, Application 21, and API integration 36 tests.
