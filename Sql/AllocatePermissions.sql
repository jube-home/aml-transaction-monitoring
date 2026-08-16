insert into "RoleRegistryPermission" ("RoleRegistryId", "PermissionSpecificationId", "Active", "CreatedDate", "CreatedUser", "Version", "Guid")
select r."Id"            as "RoleRegistryId",
       p."Id"            as "PermssionSpecificationId",
       1::smallint       as "Active",
       NOW()             as "CreatedDate",
       'Administrator'   as "CreatedUser",
       1::smallint                as "Version",
       gen_random_uuid() as "Guid"
from "PermissionSpecification" p,
     "RoleRegistry" r
where r."Id" = 2