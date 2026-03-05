---
layout: default
title: Creating Status Elevation
nav_order: 11
parent: Case Management
grand_parent: Configuration
---

🚀 Speed up implementation with hands-on, face-to-face [training](https://www.jube.io/jube-training) from the developer.
💬 Join the [Jube WhatsApp Public Support Group](https://whatsapp.com/channel/0029Vb7HM7yICVfihDH17H2P) to chat with the
developer.

# Creating Status Elevation
Consider the scenario where a rule creates a case having allocated a status of FLD (First Line Defence) as a consequence of an velocity rule having matched.  Thereafter,  suppose a transaction occurs in a sanctioned country, which is of course a much higher risk event than a velocity having been breached.  Status elevation is able to change the status in an upward,  more severe direction, on rule match, yet not allow for the status to be changed to a less severe,  downward direction.

Navigate to Models >> Cases Workflows >> Cases Workflow Status, navigating to the status First Line Review:

![Image](FirstLineReviewStatus.png)

Note the Priority as being Ultra Low.

Navigate after to the status Reported Cases Workflows Status:

![Image](ReportedStatus.png)

Note the Priority as being Ultra High.

To demonstrate the status elevation, identity a case key and case key value that is currently open in the status FLD by navigating to the cases page, navigating to the case workflow and selecting the case workflow filter All Open:

![Image](CasesSelectionSelectedLowPriorityStatus.png)

Navigate to Case Id 1:

![Image](ShowingStatusCodeIsLowPriorityForCase.png)

Make a note of the Case Key Value from the status bar,  in this case AccountId = Test1.

If the case exists in all but a closed status, another case may never be created for a Case Key Value combination.  However,  it is possible to raise the status code priority based on subsequent rule matches.

Navigate to Models >> Activation >> Activation Rules and navigate to the existing rule Case Creation:

![Image](NavigateToActivationRule.png)

Scroll down to the case creation parameters:

![Image](LocationOfCasesInActivationRule.png)

Change the Case Workflow Status Priority to Reported:

![Image](ChangingStatusCode.png)

Scroll down and update a new version of the Activation Rule:

![Image](UpdatedVersionOfActivationRule.png)

Synchronise the model via Entity >> Synchronisation and repeat the HTTP POST to endpoint [https://localhost:5001/api/invoke/EntityAnalysisModel/90c425fd-101a-420b-91d1-cb7a24a969cc](https://localhost:5001/api/invoke/EntityAnalysisModel/90c425fd-101a-420b-91d1-cb7a24a969cc) for response as follows, noting AccountId to Test1:

![Image](PostingTest1AccountId.png)

To test the status elevation, identity a Case Key Value combination that is currently open, in the status FLD, by navigating to the Cases page, then navigating to the case workflow, and finally selecting the case workflow filter All Open:

![Image](ExampleOfCaseStatusElevation.png)

Notice for the cases grid that the case has been elevated to a High status from its original default status (as identified by a change in colour and upward classification of the Priority) or in expanding the case:

![Image](ExpandedCaseShowingReported.png)