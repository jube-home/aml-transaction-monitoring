---
layout: default
title: Working Uploading and Recalling Files
nav_order: 23
parent: Case Management
grand_parent: Configuration
---

🚀 Speed up implementation with hands-on, face-to-face [training](https://www.jube.io/jube-training) from the developer.
💬 Join the [Jube WhatsApp Public Support Group](https://whatsapp.com/channel/0029Vb7HM7yICVfihDH17H2P) to chat with the
developer.

# Working Uploading and Recalling Files
It is possible to upload files to a Case Key Value combination,  with these files being centrally stored in the database.

Navigate to a case record via either Fetch or Skim:

![Image](CaseReadyForFileUpload.png)

Notice the Case Uploads tab:

![Image](LocationOfCaseUploadsTab.png)

Click on the Case Uploads tab:

![Image](ExposedCaseUploadsTab.png)

In the case upload tab there exists a file upload pane,  which will accept a dragged and dropped file,  or the specification of a file location by clicking on the Select Files button:

![Image](LocationOfFileUploadButton.png)

In this example, click on the Select Files button, the Select Files dialogue box will appear:

![Image](FileUpload.png)

Use the Select Files dialogue box to navigate to, and select a file, in this case an image:

![Image](UploadedFile.png)

The file will be uploaded and available in the database (not the file system):

```sql
select * from "CaseFile"
```

![Image](FileInDatabase.png)

The file list is all files rolling up to the Case Key Value combination. To reference or retrieve an uploaded file,  note firstly that the file name is a link:

![Image](LinkToDownloadFile.png)

Click the link:

![Image](FIleOpenInNewTab.png)

The file will open up in a new browser tab is the content type is supported,  else downloaded.