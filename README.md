![Build Status](https://github.com/evilguest/mordent/actions/workflows/dotnet.yml/badge.svg?branch=main) 
# Mordent
Mordent is a fully managed relational database management system for .Net.
This is an open-source research project startign from scratch.

I am  going to start with writing down the ideas behind this system; and, eventually, build some code to implement those ideas.

## Manifesto
Here is the list of design ideas that are floating around in my head:
### External surface
1. The server would expose everything via RESTful API. 
   **Undecided yet**: how to express the query part. ODATA? RQL? custom?
   1. "Tables": GET https://<server>/<database>/<relation>[?query]
   2. "Queries": GET https://<server>/<database>?query
   3. Read "Procedures": GET https://<server>/database>/<procedure>[?Parameters]
   4. Modifying "Procedures": POST https://<server>/database>/<procedure>[?Parameters]
2. Authentication: OAuth; consider an RBAC security model with GRANTs, DENYs and REVOKEs as usual
3. Transactions: one command - one transaction. No way to declare a transaction other than to send a complete query and wait for the response.

### Programming Model
1. We need a full-fledged language for data processing and logic management. C# would be a perfect fit if we can restrict it to something reasonable.
2. Each database is a collection of data + code. Code lives in a "project"; project might depend on another "project" which is a specific database version. 
3. Migrations are still underdesigned - need to think that through. TODO: talk to the people maintaining large and old relational databases; what is their preferred way to handle migrations? We should build a similar counterpart. 
4. There would be various objects in the database:
   1. "Tables": C# records, supporting inheritance
   2. "Views" aka table-valued functions; defined in terms of queries over the rest of the database (NB: how to prevent circular dependencies?)
   3. "Procedures" aka code blocks with parameters that might apply some changes to the code
   4. "Queues": special tables that don't allow arbitrary selection - only "consumption" and "population". Idea is that adding to the queue might be a part of one transaction, while processing the queue might be a part of a different transaction. 
   5. Custom scalar functions
   6. Custom aggregates: special types that are similar to the [CLR User-Defined Aggregates](https://docs.microsoft.com/en-us/sql/relational-databases/clr-integration-database-objects-user-defined-functions/clr-user-defined-aggregates?view=sql-server-ver15), but with the decent implementation based upon generics
   7. Indexes - indexes are supposed to be outside of the database schema, so we can add and remove them on the fly.
### Low-level implementation
1. We would use a memory-mapped file and the struct types to represent the stored values.
2. We will require x64 to avoid the addressability issues.
3. The engine would generate the struct types for storing the user-defined data automatically, during the schema load.
4. When processing a query expressed in terms of the user-defined and user-visible types, the engine would convert it to the "query plan" which is an imperative code implemented in terms of the internal struct types.
5. The database structure would be represented as a memory-mapped file, treated as an array of DbPages.
   1. Most operations on database would receive the ```Span<DbPage>``` parameter, and operate on that span. (TODO: verify the performance penalties for the range-checking; expectation is to have those negligibly small)
   2. Different types of DbPages will coexist in a form of "Union", i.e. internal blocks explicily overlayed via the ```FieldOffset``` attribute. This is to avoid casts between various ```Span<DbPageXXX>``` types
6. Strings would be stored in form of an in-row prefix, optionally followed by the B+-Tree based implementation. See [strings.md](./strings.md) for detail

