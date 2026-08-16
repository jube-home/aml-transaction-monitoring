---
layout: default
title: Activation Rules Suppression
nav_order: 18
parent: Models
grand_parent: Configuration
---

🚀 Get to pre-production in weeks, not months, with private [training](https://www.jube.io/jube-training) direct from Jube's developer — real sovereignty, zero vendor lock-in.

# Activation Rules Suppression
In the previous procedure the Response Elevation was set as the result of an Activation Rule matching.  Response Elevations are commonly used initiate the decline of a transaction in real time.

Consider the scenario where a customer has been declined in real time for foreign transactions,  yet it transpires upon investigation,  that that the customer is on holiday.  In such a circumstance,  it would be desirable to suppress any actions as the result of an Activation Rule match,  for that account.

To help illustrate, repeat the HTTP POST to endpoint [https://localhost:5001/api/invoke/EntityAnalysisModel/90c425fd-101a-420b-91d1-cb7a24a969cc](https://localhost:5001/api/invoke/EntityAnalysisModel/90c425fd-101a-420b-91d1-cb7a24a969cc) for response as follows:

![Image](ResponseElevationDrivingDecline.png)

It can be seen that there is a Response Elevation being returned, which would be inferred as a transaction decline.

Note that the IP in the JSON transaction message is "123.456.789.200":

![Image](RequestJsonShowingIP.png)

To enable the IP for Suppression matching,  navigate to the IP in the Request XPath:

![Image](RequestXPathIP.png)

To allow IP to be used in Suppression, toggle the Enable Suppression switch:

![Image](ToggleEnableSuppressionIP.png)

Update the Request XPath to create a new version including the Enable Suppression flag:

![Image](UpdatedToEnableSuppression.png)

Synchronise the models to ensure that suppression is recognised in the real-time processing. Suppression is available in the top level menu:

![Image](SuppressionMenuItem.png)

To add a suppression at the model level,  which will suppress on all rules,  navigate to the Suppression page:

![Image](SuppressionPage.png)

It can be seen in the Suppression Key drop down that the distinct list of all Request XPath elements designated for suppression:

![Image](DropDownOfSuppressionKeys.png)

It follows that the suppression value must be for the IP for which suppression is required. To add a suppression for the IP,  simply type in the IP in the Suppression Key Value:

![Image](SearchForIPInSuppression.png)

Click the Fetch button to return suppression for the IP 123.456.789.200:

![Image](ShowSuppressButton.png)

Upon clicking the Fetch button,  all models will be returned for which IP is eligible, overlay with a switch to indicate the suppression status for the IP 123.456.789.200:

![Image](SuppressionReturnForIPModel.png)

To suppress for the IP value 123.456.789.200,  simply toggle the switch next to the model name to indicate that all Activation Rules belonging to the model should be suppressed:

![Image](IPNowSuppressedForIP.png)

Next to the toggle switch,  a Delete Expiry Date column is exposed,  using a date and time picker.

The Delete Expiry Date picker is disabled unless suppression is currently switched on for the row,  becoming immediately available the moment the switch is toggled on,  and immediately locked again,  and cleared,  the moment the switch is toggled off. Setting a Delete Expiry Date has exactly the same effect as manually toggling the switch off once that date and time is reached: it does not require an explicit synchronisation or any further manual intervention. Leaving Delete Expiry Date blank,  which is the default the moment a suppression is switched on,  means the suppression never expires and must be removed manually by toggling the switch off. The date and time entered must be in the future; an attempt to set a value in the past is rejected immediately in the browser,  reverting the picker to its previous value and displaying a validation message on the page,  without the invalid value ever being sent to the server. The same Delete Expiry Date column,  with the same behaviour,  is available for suppression scoped to a specific Activation Rule,  described further below.

Every change made to a suppression record,  whether removed by toggling the switch off or by a Delete Expiry Date being reached,  is recorded to an audit history,  in the same manner as other versioned objects in Jube.

Suppression (as Lists and Dictionary) do not require an explicit synchronisation,  rather they will be synchronised in the engine as a matter of routine.

Repeat the HTTP POST to endpoint [https://localhost:5001/api/invoke/EntityAnalysisModel/90c425fd-101a-420b-91d1-cb7a24a969cc](https://localhost:5001/api/invoke/EntityAnalysisModel/90c425fd-101a-420b-91d1-cb7a24a969cc) for response as follows:

![Image](ActivationsStillMatching.png)

It can be seen that the Activation Rule has still matched - which is to be expected - but note that the following actions are suppressed:

* Response Elevations.
* Notifications.
* Case Creation.
* Activation Watcher.

TTL Counter increments will not be suppressed.

In the response,  locate the Response Elevation:

![Image](ResponseElevationZero.png)

It can be seen that the Response Elevation is zero - as it has been suppressed - and will no longer drive a decline (being a Response Elevation value other than zero).

To remove the suppression,  simply toggle off the switch after having fetched the IP of 123.456.789.200 from the suppression tab:

![Image](ToggleOffSuppressionAtModelLevel.png)

To validate removal of suppression, repeat the HTTP POST to endpoint [https://localhost:5001/api/invoke/EntityAnalysisModel/90c425fd-101a-420b-91d1-cb7a24a969cc](https://localhost:5001/api/invoke/EntityAnalysisModel/90c425fd-101a-420b-91d1-cb7a24a969cc) for response as follows:

![Image](SuppressionRestored.png)

It can be seen that the Response Elevation is now active - given that the suppression has now been removed.

It is also possible to specify just a specific activation rule for suppression.

To expose the Activation Rules rolling up to model,  click on the arrow to the left hand side of the grid:

![Image](ExpandSuppressionForActivationRules.png)

Click on the arrow to expand Activation Rules:

![Image](ExpandedActivationRuleSuppression.png)

The suppression process works in the same manner as at the model level with a simple toggle switch to be immediately synchronised,  but suppression will be targeted to a rule name and not encompass the whole model processing of the transaction or event.

As at the model level,  a Delete Expiry Date can optionally be set once the switch is toggled on for the Activation Rule,  behaving identically: an automatic,  unattended un-suppression at the date and time specified,  requiring no further synchronisation.