---
layout: default
title: Creating Case Entry With Activation Rule
nav_order: 4
parent: Case Management
grand_parent: Configuration
---

🚀 Speed up implementation with hands-on, face-to-face [training](https://www.jube.io/jube-training) from the developer.
💬 Join the [Jube WhatsApp Public Support Group](https://whatsapp.com/channel/0029Vb7HM7yICVfihDH17H2P) to chat with the
developer.

# Creating Case Entry With Activation Rule
Revisit Activation Models Activation and Reprocessing in this documentation before proceeding.

To create a case record as a consequence of an Activation Rule match, navigate to the Volume1DayUSDForIPOver100 created beforehand:

![Image](ExistingActivationRuleToCreateACase.png)

Scroll down to Cases checkbox in the Activation Rule:

![Image](LocationOfEnableCases.png)

Check Cases to expose the options to create a case as a consequence of this Activation Rule matching:

![Image](ExposedCreateCaseInActivationRule.png)

For the time being,  do not adjust the Bypass parameters,  yet note that the default case creation parameters are the Case Workflow and Case Workflow Status created in the previous procedures.

The Case Key refers to the value that will be used to distinctly create the case,  whereby the case creation integrity will be enforced on that key.  For example,  if a Case Key is AccountID,  only a single case record can be created for for a Case Key Value combination,  for a case workflow.

Select parameters as follows:

![Image](ValuesToCreateACaseByAccountId.png)

Scroll down the page and click Add to create a version of the Activation Rule:

![Image](VersionOfActivationRuleWithCaseCreation.png)

Synchronise the model via Entity >> Synchronisation and repeat the HTTP POST to endpoint [https://localhost:5001/api/invoke/EntityAnalysisModel/90c425fd-101a-420b-91d1-cb7a24a969cc](https://localhost:5001/api/invoke/EntityAnalysisModel/90c425fd-101a-420b-91d1-cb7a24a969cc) for response as follows:

![Image](ActivatedForCaseCreationInResponsePayload.png)

It can be seen from the response that the Case Creation Activation Rule has activated,  which will have driven the creation of a case.

To view the created case,  navigate to Cases:

![Image](CasesAvailable.png)

If not already selected by session,  click on a Case Workflow Filter to display open cases:

![Image](ClickingOnCasesWorkflowFilter.png)

It can be seen upon matching,  a case has been created.

![Image](CasesOnExplictFilterSelect.png)


