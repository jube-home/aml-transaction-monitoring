---
layout: default
title: Lists
nav_order: 5
parent: Models
grand_parent: Configuration
---

🚀 Get to pre-production in weeks, not months, with private [training](https://www.jube.io/jube-training) direct from Jube's developer — real sovereignty, zero vendor lock-in.

# Lists
The Lists Page facilitates the creation of lists of data that can be referenced as an operator in all rules in the models.  For example, the Lists page can be used to maintain a list of bad customer account identifiers that can be referenced using the Builder or Coder.

Lists might for example be used to maintain large Deny and Accept lists, without having to embed large amounts of complexity into rules,  instead embedding only the list reference.

Lists are synchronised by the engine in the background constantly, and they do not require an explicit synchronisation.  It follows that any new list value is available more of less straight away.

The page is available by navigating to the menu item Lists, in the main page.  The positioning of the List navigation is in the main section,  even though it is in fact part of models, for the purposes of delegating this otherwise administrative function to customer service orientated roles:

![Image](MenuItemList.png)

Click as customary to expose a model tree which takes a similar form as other model administration objects:

![Image](ListsTopOfTree.png)

To create a list,  click on the Model in the tree towards the left hand side:

![Image](EmptyListReadyForAdd.png)

Creating a List by the name Bad Customers:

![Image](ExampleListBadCustomers.png)

Click the Add button to confirm creation of the List:

![Image](AddedListOfBadCustomers.png)

The List values are maintained using a grid facilitating the addition, updating and removal of List Values:

![Image](ChildrenValues.png)

To add a value to the list,  a button is implemented titled 'Add Value':

![Image](AddValueButton.png)

Clicking the Add button will overlay a form and create a new entry in the grid:

![Image](TemplateForAddValueToList.png)

To commit the new value,  click on the tick icon:

![Image](TickConfirmButton.png)

Or to cancel the creation of the Value click the opposing icon:

![Image](CircleCancelButton.png)

Include the value as follows in the input box:

![Image](ExampleValueForListValue.png)

The Add/Update Value form also exposes an optional Delete Expiry Date field, alongside the List Value, using a date and time picker.

Delete Expiry Date has the same effect as deleting the List Value: once the date and time entered is reached, the value is automatically excluded from processing and from the grid,  without an explicit delete ever being actioned. Leaving Delete Expiry Date blank,  which is the default,  means the value never expires and will need to be removed manually if it is to stop being available. The date and time entered must be in the future; an attempt to set a value in the past is rejected by the server, with the reason displayed above the grid.

This applies to any field on the List Value entry, not only Delete Expiry Date: an empty List Value is likewise rejected server-side. On rejection the entry is automatically re-opened for editing and highlighted in red, in addition to the message shown above the grid, making it clear exactly which entry needs correcting.

To update a List Value,  click on the pen icon for the list value to be updated. Upon clicking the Pen icon:  

![Image](EditButtonExistingListValue.png)

A form with a text box will be overlay:

![Image](OverlayListItemForEdit.png)

Update the text box with the new value:

![Image](UpdatedExistingListValue.png)

Commit by clicking on the Tick icon:

![Image](TickConfirmButton.png)

Or to cancel the edit of the Value click the opposing icon:

![Image](CircleCancelButton.png)

Upon the successful commit of the Value,  it will appear updated in the List control:

![Image](UpdatedListValue.png)

To delete a List value entry,  it is a simple matter of clicking on the Disk Icon next to the List Value targeted for deletion:

![Image](DeleteButton.png)

A confirmation of the delete will be sought:

![Image](ConfirmDeleteOfListValue.png)

Upon confirming the delete,  it will be removed from the list of values:

![Image](ValueDeleted.png)

It is also possible to upload a list of values by uploading a CSV file given the downloadable template in the page.

Note there is no column header in the above file and structure. Each line is a List Value, optionally followed by a comma and a Delete Expiry Date. The Delete Expiry Date must be in .NET's round-trip ("O") format, for example `2027-01-01T00:00:00.0000000Z` - this is enforced strictly, with no other date format accepted, to avoid the day/month ambiguity inherent in free-form date parsing of a text file. Where the Delete Expiry Date is omitted or left blank, the List Value never expires. A template CSV file, illustrating both a List Value with no expiry and one with a Delete Expiry Date, can be downloaded directly from the List page using the Download CSV Template link found next to the Select Files control.

The control to upload a file is the button Select Files:

![Image](SelectFilesButton.png)

Either drag the file to the area shown above,  or click the button titled Select files,  navigating to the file:

![Image](SelectFile.png)

Clicking Open, which will happen implicitly if the file has been dragged and dropped, will both upload the file and process,  adding each row as a new list entry:

![Image](UploadedListValues.png)

The list and its values will now be available to rules,  for example:

![Image](ListAvailableInGatewayRule.png)

