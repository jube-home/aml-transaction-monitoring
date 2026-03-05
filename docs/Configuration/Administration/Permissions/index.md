---
layout: default
title: Permissions
nav_order: 3
parent: Administration
grand_parent: Configuration
---

🚀 Speed up implementation with hands-on, face-to-face [training](https://www.jube.io/jube-training) from the developer.
💬 Join the [Jube WhatsApp Public Support Group](https://whatsapp.com/channel/0029Vb7HM7yICVfihDH17H2P) to chat with the
developer.

# Permissions
Permissions grant access to resources in the user interface,  typically pages.  Permissions are added to Roles,  and then a Role is allocated to a user.

In practice,  it is unlikely that a user with day to day responsibilities in customer services would require access beyond Case Management, Lists, Dictionary or Suppression.  Similarly,  an analyst that has responsibility for the creation of rule and reports is unlikely to need access to those pages enjoyed by customer service.

In the absence of a permission the user will not be able to navigate given absence of the resource in the menu.  In the event of deep linking to a resource without a permission,  an unauthorised http status code will be returned.

The Administrator role is created during migrations and has all available permissions allocated.  To view permissions for the Administrator role,  start by navigating Administration >> Security >> Permissions:

![Image](TopOfTreeForPermissions.png)

Locate the triangle to bring about an expansion of the tree,  to expose the permissions for the Administrator role:

![Image](LocationOfExpandButton.png)

Click to expand permissions for the Administrator role:

![Image](ExpandedPermissions.png)

Locate the Read Write Case permission for the Administrator:

![Image](LocationOfReadWriteCasePermission.png)

Click on the Read Write Case permission to expand on the properties:

![Image](ExpandedReadWriteCasePermission.png)

Deleting this permission will render the Case pages inaccessible:

![Image](LocationOfCasesMenuItem.png)

Locate the Delete button:

![Image](LocationOfDeleteButton.png)

Click the Delete button and confirm:

![Image](EntryDeleted.png)

Refreshing the whole page will construct the menu without the Case menu item for absence of permission:

![Image](AbsenceOfCases.png)

To establish the permission once again navigate to back to Administration >> Security >> Permissions, and click on the top on the tree node Administrator to create a new entry:

![Image](EmptyPermission.png)

To add a permission is to select the permission from the drop down of available permissions.  Locate the permissions drop down:

![Image](LocationOfPermissionSpecification.png)

Select Read Write Case and ensure Active: 

![Image](AddBackPermissionForCase.png)

Click Add to create a version of the permission:

![Image](VersionOfPermission.png)

As permission may not be added twice.  In the event the permission has already been allocated to the Role,  the existing entry will be updated for the new properties.  With the permission resetablished to access the Cases,  a refresh of the whole page will make available the menu item:

![Image](PermissionRestablished.png)

