#Sharp Tibia Client
This is a protocol 7.4 compatible Tibia client written entirely in C#. This is a mostly abandoned project I did for fun several years ago, as such it is not complete by any measure.

#Feature support
Right now, it supports opening Tibia movie files and replaying them pretty much perfectly. With support for chat, animations (except walking), inventory etc. Connecting to a network host is the very simple matter of changing the supplied stream to a network stream instead of a file stream.

There is little-to-no support for input yet. But since all the send* functions are there it should a relatively small project to implement.

#Design
The design is very modular, the Protocol/ folder contains parser for the tibia protocol and data files and exposes events for all the actions available.

The Client/ folder contains information about the current client state. It hooks into the events supplied by the Protocol and updates it's own internal data structure. This means it's trivial to fast-forward in a protocol stream by feeding it as many packets as you'd like (ClientState offers this functionality).

The GameCanvas class separately hooks into the events provided by the protocol in order to display effects. When fast-forwarding you can connect the Canvas afterwards in order to skip parsing of effects entirely!

#Protocol
It only support client 7.4 right now (mostly because I didn't feel like implementing RSA). It's coded with multi-protocol support in mind though, so if you want to add new protocols what you need to do is edit create a new protocol map XML file, and add new parsing delegates in TibiaGameParserFactory.cs.

#Contributors
* Moi, Hampus Nilsson, or Remere for short. :)

Hope somebody finds use for this!