---
layout: default
title: Case Workflow Action
nav_order: 7
parent: Case Management
grand_parent: Configuration
---

🚀 Get to pre-production in weeks, not months, with private [training](https://www.jube.io/jube-training) direct from Jube's developer — real sovereignty, zero vendor lock-in.

# Case Workflow Action
Case Notes provide a means to annotate cases and provide a history of the unstructured actions taken.  Case notes by their nature are unstructured and cannot be reported upon. There are two structured data items that can be attached to a case note:

* Priority (High,  Medium and Low) and;
* Action Codes which are configured rolling up to the Cases Workflow.

In addition to providing typing and structure to unstructured notes,  Action Codes can also drive Notifications and integrations \ automation.

To create an Action Code,  begin by navigating to Models >> Cases Workflows >> Cases Workflow Action:

![Image](CasesWorkflowsActionTopOfTree.png)

Expand the first level of the tree and click on the model to start a new entry:

![Image](EmptyCasesWorkflowAction.png)

Actions are extremely basic in comparison to other child elements available to the case workflow,  really only taking a generic parameter of Name and the common parameters to drive HTTP Endpoints of Notifications.

Complete the form as follows:

![Image](ExampleValuesCasesWorkflowAction.png)

Scroll down and click the Add button to create a version of the Cases Workflow Action:

![Image](VersionOfCasesWorkflowAction.png)

Navigate to a case:

![Image](DefaultCasePage.png)

To allocate the newly created Action, locate the Case Notes tab:

![Image](LocationOfCaseNotesTab.png)

Click on the Case Notes tab to expose recent notes, and the ability to create a new entry:

![Image](ExposedCaseNotes.png)

Locate the text editor section to create a new Note:

![Image](LocationOfTextEditor.png)

Type into the text editor as required:

![Image](ExampleNote.png)

On typing, the Add Note button will be enabled:  

![Image](LocationOfAddNote.png)

Alongside the add button there is a drop down containing the newly created Cases Workflows Action:

![Image](ActionDropDown.png)

Having set the values,  click on the Add Note button to create the note:

![Image](NoteAdded.png)

Notice how the note had been added and the Action and Priority recorded alongside.