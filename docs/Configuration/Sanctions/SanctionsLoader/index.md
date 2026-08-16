---
layout: default
title: Sanctions Loader
nav_order: 1
parent: Sanctions
grand_parent: Configuration
---

🚀 Get to pre-production in weeks, not months, with private [training](https://www.jube.io/jube-training) direct from Jube's developer — real sovereignty, zero vendor lock-in.

# Sanctions Loader

Sanctions are published by various bodies and provide a list of names for which business is prohibited. The
functionality offered by Jube allows the loading of Sanctions lists into the engine for matching using fuzzy logic (
Levenshtein Distance) to create matches. Jube also allows this matching to be embedded into the Entity Analysis Model
recall in real time. Sanctions data is stored in the engine, in memory, so to assure that the recall is extremely fast.

Sanctions requires the EnableSanction Environment Variable being set to True:

```text
EnableSanction=True
```

On the enabling of sanctions, a thread will be started. This thread should be started on all servers that require online
recall of sanctions, either directly or via model invocation.

There are many different types of sanctions file available, published from the EU, US, UN etc. The specification of
sanctions files are stored in the SanctionEntrySource database table the name, delimiter and presence of several
mappings detailed later in the loading process:

```sql
select * from "SanctionEntrySource"
```

![Image](SanctionsEntrySource.png)

By default there are four file definitions in place, alongside their locations on the internet:

* US Office Of Foreign Assets Control (OFAC) Specially Designated National (SDN).
* Bank of England (BOE) Sanctions.
* European Union (EU) Sanctions.
* US Office Of Foreign Assets Control (OFAC) Specially Designated National (SDN) alternative names.

New entries to the SanctionEntrySource table can be accepted as following definition:

| Value                     | Description                                                                                                                                                                                                                              | Example                                         |
|---------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------|
| Name                      | The reference for the sanctions source.                                                                                                                                                                                                  | SDN                                             |
| Severity                  | An integer number to express notional severity.                                                                                                                                                                                          | 1                                               |
| Directory Location        | In the absence of internet or HTTP polling the local file system directory to poll for same file.  All files in the directory will be consumed.  Intended for use only on the basis that security disallows access to the HTTP resource. | /Sanctions/SDN                                  |
| Delimiter                 | The delimiter separating the field in the file.                                                                                                                                                                                          | ,                                               |
| Multipart String Index    | The zero based field location for the multipart string.  I the event of many elements needing to be concatenated,  seperated with a comma, whereby concatenation will be seperated by space.                                             | 1                                               |
| Reference Index           | The zero based field location for the reference of the entry                                                                                                                                                                             | 0                                               |
| Enable Directory Location | A bit value to instruct polling of a Directory Location for new files.                                                                                                                                                                   | 0                                               |
| Enable HTTP Location      | A bit value to instruct polling of a HTTP location for latest file.                                                                                                                                                                      | 1                                               |
| HTTP Location             | The location to poll for current sanctions files.                                                                                                                                                                                        | https://www.treasury.gov/ofac/downloads/sdn.csv |
| Skip                      | The first rows to skip on account of having header data.                                                                                                                                                                                 | 0                                               |

To enable the loading process, the following Environment Variables need to be set:

| Value                | Description                                                                                                                                                                                                                      |
|----------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| EnableSanctionLoader | A flag indicating if sanctions should be loaded via this instance of the engine and inserted into the SanctionEntry database table,  noting that this table will be synchronised to memory.                                      |
| SanctionLoaderWait   | Interval in millisecond between polling and synchronizing new sanctions files.  A key concept is regular polling of the official locations with a view to perpetual validation and merging of the new records with the database. |

```text
EnableSanctionLoader=True
SanctionLoaderWait=60000
```

By default the sanctions loader process is disabled so not to make connections to the internet without explicit
instruction by the end user. Via Migration, there is only a single example sanction for Robert Mugabe loaded by default
for the purpose of documentation and training.

In enabling Environment Variables for the loader, the files will be loaded and their contents inserted into the
SanctionEntry database table:

```sql
select *
from "SanctionEntry"
```

![Image](PublicSanctionsInTable.png)

The SanctionEntry database table is the initial target for the synchronisation on loading of the instance. On perpetual
processing of sanctions files the entries will be merged. An entry is validated by creating an MD5 hash - of the source,
the multipart string value and the reference, taken together - then checking its existence in the SanctionEntry database
table, and thereafter if new, adding it to both the SanctionEntry database table and the in memory copy of it. A changed
name or reference for what is otherwise "the same" entry therefore produces a different hash, and is treated as a new
entry rather than an update to the old one.

## Import and rejection audit trail

Every run of the loader - for every SanctionEntrySource in turn - records a SanctionEntryImport row describing
what happened: total rows read, and how many were Inserted, Revived, Unchanged, Removed and Rejected, along with
whether the run was Successful and, if not, the error message. Individual bad rows are recorded to
SanctionEntryRejection with the row number, raw data and a rejection ReasonId, rather than only failing the run as a
whole:

| ReasonId | Meaning                                                                                                                     |
|----------|-----------------------------------------------------------------------------------------------------------------------------|
| 1        | InsufficientFields - the row did not contain enough delimited fields to be parsed.                                          |
| 2        | NoReferenceIndexConfigured - the SanctionEntrySource has no Reference Index configured, so no reference could be extracted. |
| 3        | ParseError - an unexpected error occurred while parsing the row.                                                            |

Only the ReasonId is stored, not the underlying exception message - rows sharing the same failure mode would
otherwise duplicate near-identical free text with no real diagnostic gain over the enum itself.

The four outcomes recorded per entry are:

| Outcome   | Meaning                                                                                                                                                                                                                                                                                                                                             |
|-----------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Inserted  | The entry's hash was not previously seen for this source - a brand new row.                                                                                                                                                                                                                                                                         |
| Unchanged | The entry's hash matches an existing, active row - no write is made.                                                                                                                                                                                                                                                                                |
| Revived   | The entry's hash matches an existing row that had previously been soft-deleted (see below) - the row is restored in place rather than re-inserted, so its Id is preserved.                                                                                                                                                                          |
| Removed   | An existing active entry for this source was not present in this run's file at all, and is soft-deleted (SanctionEntry.Deleted / DeletedDate / DeletedUser set) rather than physically removed. If a file produces zero rows at all, removal is skipped entirely for that run, so a broken or empty download cannot mass-delete a source's entries. |

Because Removed entries are soft-deleted rather than physically deleted, an entry that disappears from a published
list and later reappears (Revived) keeps its original Id and any history associated with it, rather than starting
over as a new row.

## Manual upload

In addition to the automatic polling loader above, a source's file can be uploaded manually from Administration >>
Sanctions Loader, which requires Landlord permission. This is useful where a source only publishes on an ad hoc
basis, or where outbound internet access from the Jube servers is restricted and files are instead obtained and
vetted out of band before being loaded.

Manual upload shares the same import path as the automatic loader described above - the same hash-based
Inserted/Unchanged/Revived/Removed reconciliation, and the same SanctionEntryImport/SanctionEntryRejection audit
rows are written - so a manually uploaded file behaves identically to one picked up automatically, except that
CreatedUser on the resulting audit trail records the logged-in user's name rather than the background loader.

## Stop Tokens

Before matching, common honorific and religious/cultural titles - tokens such as those equivalent to "Sheikh" or
"Imam" that are common in some naming conventions - are stripped from both the searched string and every sanction
entry, so that including or omitting such a title does not itself affect whether names match. The list of stop
tokens lives in the SanctionStopToken database table, seeded with an illustrative starting set via migration, and is
loaded into memory at the start of every loader cycle alongside sanction entries and files.