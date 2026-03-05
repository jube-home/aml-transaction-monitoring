---
layout: default
title: Working, Lock, Allocation and Unlock
nav_order: 13
parent: Case Management
grand_parent: Configuration
---

🚀 Speed up implementation with hands-on, face-to-face [training](https://www.jube.io/jube-training) from the developer.
💬 Join the [Jube WhatsApp Public Support Group](https://whatsapp.com/channel/0029Vb7HM7yICVfihDH17H2P) to chat with the
developer.

# Working Lock, Allocation and Unlock
When a cases is locked to a user,  it is owned by that user until such a time that it is unlocked or the user allocates the locked status to another user.

In the following, the Case Key Value combination AccountId = Test1 has been Skimmed, therefore automatically locked:

![Image](CaseLockedToUser.png)

Noting two case status properties, Locked and Locked User, to allocate the lock status to another user,  the Locked User drop down can be used to search for, then select, another system user in the tenant:

![Image](OtherUserToLockTo.png)

Click on a new user after which the case will update to lock to that user:

![Image](UpdatedToOtherUser.png)

To unlock a case record,  it is a simple matter of sliding the Locked toggle button to False, or unchecked:

![Image](UpdateLockedToUnlocked.png)

The case record will be updated and no longer be considered locked:

![Image](UpdatedLockedToUnlocked.png)