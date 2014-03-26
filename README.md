Regard Queries
==============

This project contains an abstraction layer and an implementation for
performing queries on events gathered by Regard.

Requirements:

* Simple API
* Easily replaceable in the first version
* Serializable
* Designable

Eventual requirements:

* Fast
* Cheap
* Scalable

The API design should support these requirements, but the implementation
need not in the first version.

Notes: events
=============

Each event contains the following items:

* Product + organisation it is for
* Unstructured data in the form of key/value pairs
* A session identifier (desired, but not currently present)

There may also be some optional but common key/value pairs, like these:

* Timestamp

These fields perhaps deserve to have a well-known format so we can write
richer queries easily.

It may be useful to have derived properties as well. For example having
something like 'Timestamp-Day' as a way of retrieving the day value from
a time stamp.

Notes: queries
==============

Queries are designed to produce aggregate results rather than
individual events. The 'basic' query simply counts the number
of events from a particular source, and can then be broken
down or restricted to produce more general aggregations.

Examples: queries
=================
 
This query will produce a break down of the number of sessions
per day (suitable for drawing a graph for instance):

    queryBuilder.AllEvents()
                .CountUniqueValues("SessionId")
                .BrokenDownBy("Day")

At least, it will assuming that your events have a SessionId and Day
property. Take away the 'CountUniqueValues' and it will count the number
of events per day. Take away the 'BrokenDownBy' and it will give you the
total number of unique sessions over the lifetime of the product.

Here's a complicated query: number of sessions that performed an event
at least once, broken down by day (suitable for generating a heat map):

    queryBuilder.AllEvents()
                .CountUniqueValues("SessionId")
                .BrokenDownBy("Day")
                .BrokenDownBy("EventType")
