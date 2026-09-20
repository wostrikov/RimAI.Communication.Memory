# The colony summary

Everything else this module remembers is pawn-shaped: what somebody recalls,
who they are, what they think of each other. Nothing was ever about the place
they all live in, so a colonist could discuss last week's quarrel in detail
while a raid burned the kitchen and never mention it.

`{{colony}}` is that missing half: one paragraph per map about how the colony
is doing — the situation, where it is heading, what threatens it.

## How it is made

    map → ColonySnapshot → prompt → model → one paragraph → {{colony}}

* **`ColonySnapshotCollector`** reads the map: colonists and how many are
  down, prisoners, average mood and how many sit below their break threshold,
  days of food at the colony's own appetite, medicine, wealth, hostiles,
  fires, season, outdoor temperature, current research, and the last six
  entries from the game's own archive — the list the History tab draws.
  All of it is state the game already keeps; the pass costs one walk over the
  spawned colonists.
* **`ColonySnapshot.Fingerprint()`** rounds those numbers to the nearest step
  that matters. Mood to a tenth, wealth to a thousand, temperature to five
  degrees. That is deliberate: an exact figure changes every tick and would
  turn the fingerprint into a clock.
* **`ColonyTrendManager`** is a `GameComponent`, so there is one per save. It
  checks every 2500 ticks and asks the model only when both are true — the
  interval has passed *and* the fingerprint has moved. A paused game or a
  quiet afternoon costs nothing.
* The answer is flattened to one paragraph, cut to the configured length, and
  scribed, so a reload starts with the last answer rather than a blank.

## What it costs

One AI request per map, at most once per the configured interval, and only
when something changed. At the default six hours that is four requests a day
in the worst case and usually fewer. It goes through the same
`IndependentAISummarizer` path as the rest of the module, so it uses whatever
provider the module is already configured with.

It is **off by default** for exactly that reason. Options → Mod settings →
Memory → Advanced → *Colony summary*.

## Using it

Put `{{colony}}` in a prompt. It is a context variable rather than a pawn one:
there is a single answer per map and every speaker on that map shares it.

    The colony right now: {{colony}}

An empty string is the honest answer before the first summary exists, so a
prompt that uses it should read sensibly without it.

## The instruction

`colonyTrendInstruction` in settings holds the wording sent to the model; blank
means the default in `ColonyTrendPrompt.DefaultInstruction`. The facts beneath
it are built as plain `label: value` lines rather than JSON — a model asked for
a sentence answers with a sentence more readily when it was not handed a data
structure.

## Where this came from

Two RimTalk add-ons were weighed for adoption on 2026-09-19 and both were
refused: they hard-depend on RimTalk, which this modset replaced with RimAI,
and running both would mean two dialogue systems talking over each other. One
idea in them was worth having anyway, and this is it — reimplemented here
rather than merged, which is what this project does with donor ideas.
