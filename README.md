Regard Queries
==============

This project contains an abstraction layer and an implementation for
performing queries on events gathered by Regard.

Requirements:

    * Reasonably simple
    * Easily replaceable in the first version
    * Serializable
    * Designable

Eventual requirements:

    * Fast
    * Cheap
    * Scalable

Notes: events
=============

Each event contains the following items:

    * Product + organisation it is for
    * Data in the form of key/value pairs
    * A session identifier (desired, but not currently present)

There may also be some optional but common key/value pairs

    * Timestamp

Notes: queries
==============

It's desirable to avoid raw SQL queries in the implementation of Regard as
this would tie us to a single data implementation, which would limit
our options when we want to scale things out.

The main goal of this project is to put everything that accesses the
event database in a single place, so it is easy to find, review and
if necessary change or replace.

A secondary goal is to help with the way that queries are designed
and used, by providing an abstraction that works in a way that mirrors
that of the developer dashboard.

Services provided by this library to further these goals:

    * Fake data to aid with visualising queries before there are any
      events associated with them (Regard requires that queries be
      designed up front).
    * Enforcement of any anonymity guarantees that we might provide
