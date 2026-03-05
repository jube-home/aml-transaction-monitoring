---
layout: default
title: Multi Tenancy Concepts
nav_order: 1
parent: Multi Tenancy
grand_parent: Concepts
---

🚀 Speed up implementation with hands-on, face-to-face [training](https://www.jube.io/jube-training) from the developer.
💬 Join the [Jube WhatsApp Public Support Group](https://whatsapp.com/channel/0029Vb7HM7yICVfihDH17H2P) to chat with the
developer.

# Multi Tenancy Concepts
Jube supports multi-tenancy which allows single infrastructure to be shared among many logically isolated clients (i.e. customers rather than end users rolling up to those customers), maintaining total isolation between tenants data,  with no loss of function in the user interface.  Given the use of Environment Variables,  optionally, Jube threads and cache offloading can be quite selective also,  which serves to separate compute where required,  why obtaining cost efficiency of centralised configuration.

In Jube,  isolation between tenants is rooted at a TenantRegistryId field at the top of various data hierarchies which are joined and predicated for in every query made against the database.

Behind the scenes, for each request made via the API, the tenant registry identifier for the authenticated user is looked up,  with that value being used as predication in any database query.  For example,  suppose the table:

``` sql
select * from "EntityAnalysisModel"
```

Returning the follow data:

![Image](EntityAnalysisModelData.png)

Note the field TenantRegistryId:

![Image](NoteTenantRegistryId.png)

The tenant registry identifier implies that all of the data in this table is owned by TenantRegistryId 1. The EntityAnalysisModel has many child tables which have relationships,  henceforth it is not necessary to have the TenantRegistryId on each table,  as it can be joined to.  For example:

``` sql
select * from "EntityAnalysisModel" m
inner join "EntityAnalysisModelRequestXpath" x on x."EntityAnalysisModelId" = m."Id"
where m."TenantRegistryId" = 1
```

The above pattern is applied to every query made available to the API, this for the user interface, having the effect of total isolation between tenants,  not withstanding the same physical database instance (unless Cache and Threads are arranged otherwise with Environment Variable configurations).