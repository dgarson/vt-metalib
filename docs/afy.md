# `afy` YAML Meta Definition (top-level)

# Meta
The top-level object defined in an `afy` file is the `Meta` object and contains, at minimum, some name information and optional free-form key-value pairs for labels, etc.

Underneath the `Meta` are sections to define reusable `NavRoutes` as well as each of the `States` within the meta and its associated `Rules`. 

For more advanced users, there is also the ability to import templateable fragments, or portions of a meta, using the `MetaFragment` and/or `StateFragment` features.

## Properties
### `ResetState`
`ResetState` is an optional string (defaults to `Default`) that can be defined for use in other parts of the meta where a hard "meta reset" is desired and the meta being defined wishes to have more control over that transition.
### `StateFragments`
`StateFragments` is an optional list of State Fragments (explained later in doc) with a declared `Name` and `Path` for each
### `MetaFragments`
`MetaFragments` is an optional list of Meta Fragments (explained later in doc) with a declared `Name` and `Path` for each

## Fragments
Fragments provide a meta developer to write templatable portions of a meta that can be imported into a concrete `afy` meta file.
Each fragment is defined externally in a separate `afyf` (afy fragment) file which has a different format from `afy` depending on which tyepe of fragment.
Every fragment must be declared with a unique `Name` that is referenced throughout the `afy` file. Meta fragments may also provide an optional `StateNamePrefix` that will be prepended to any states generated thru the import statement.

Since fragments are used as templates, there are variables provided to each fragment during the import process. The templating process uses standard Go Templating syntax. 
  Specs: https://pkg.go.dev/text/template
  References: https://learn.hashicorp.com/tutorials/nomad/go-template-syntax

There are two types of fragments:
* Meta Fragments: Imported on the meta-level. Barring top-level metadata, a the structure/syntax of a meta fragment is almost identical to that of a typical (concrete) `afy` file.
* State Fragments: Imported into a state that is actively being defined in the `afy` file. State fragments are broken into sections which are referenced by name. Each section, when imported, produces zero or more Rule entries that are inserted into the importing state based on the position of the ImportFragment directive. 

Additional concepts introduced with fragments:
* Reset State: optional declared state name that should be transitioned to if a complete "reset" of the meta is required, defaults to the "Default" state
* Vars to Clear: since a fragment may induce a meta state change, fragments must be capable of performing var-management without knowing the particular implementation of the state. In order to support this, developers may provide fragments with a list of vars and/or pvars that should be cleared automatically in any fragment-induced state change (see below for auto-provided var names for Go templates).

## Nav Routes

Nav routes are declared at the Meta level in the optional `NavRoutes` key and are declared by name so they can be referenced in a reusable fashion within any `EmbedNav` Action within the meta.

Nav routes can be defined in multiple ways, based on additional keys beyond the `Name`. Every type of Nav Route defined below has a couple optional properties that may be defined:

### Common nav route parameters

#### `ReversePoints`
`ReversePoints: True|False` allows for the reversal of the points in the Nav Route being defined

#### `Transform` block allows for automatic transformation of the defined Nav Route, e.g. coordinate offset and/or rotation.
```
- Name: "EOMid"
  Path: "navs/eowest.nav"
  Transform:
  	Translate:
  		dx: 2.10299
  		dy: 6.11059
  		# optional
  		dz: 0.0000
  	Rotate: 72
```
Both the `Translate` (direct coordinate offset) as well as the `Rotate` properties can be used independently or in conjunction with one another. Only one is required in order to use the `Transform` feature.


### External nav routes
The most commonly used form is a simple import from an external nav route, e.g.
```
- Name: "GoToBot"
  Path: "navs/GoToPortalBot.nav"
```

### Inlined nav routes
If you declare a `Type` key, then you are able to inline nav points similar to how one might do using the original `af` file format:
```
- Name: "NAVNAME"
  Type: Linear|Circular|Once
	Points:
	- Point: 0.05127773, 65.46781111, 0.092765523
	- Chat: hello world!
	- Vendor: Mr Supplier Npc
```

### Inline follow nav route
```
- Name: "FollowLeader"
  Type: Follow
  Character: "Alastor"
```

### Embedded data routes
Although discouraged in most use cases, a developer can also inline the entire `nav` file contents in their `afy` file by using the `Data` key and providing a string value (recommended to use a string literal block as shown below)
```
- Name: "EmptyNav"
  Data: |
  	uTank2 NAV 1.2
		4
		0
```
(NOTE: YAML string literal blocks read lines exactly as-is and are terminated when a line is encountered with fewer indentation or when the end of file is reached)


# States
States are defined within the `Meta` by a unique `Name` key and are composed of one or more entries in the `Rules` property.
Additional properties may become available / optionally provided within each `States` list entry.

# Meta Rules

Rules are defined by providing a list item in the `Rules` property that contains one `Condition` value as well as one `Action` value. Instead of providing a Condition and an Action, a new directive has been introduced for use with State Fragments (see below), since this translates to zero or more `Condition`/`Action` pairs that can be inserted in its place.

Since YAML is used to define `afy` files, the flexible syntax allows for additional optional parameters to be defined within a Rule. For now, this includes an optional `Label` that identifies the rule uniquely within its encapsulating State (not within the entire meta), and additional optional properties may be added later.


## Conditions
Conditions typically begin a list item in the `Rules` property of an entry in `States`. They begin with a common syntax that must define the Condition Type, since different condition types will have different expected keys/property values for any parameterized conditions.
```
- Condition: CONDTYPE 
  ...
  ...
```

Note that due to `afy` using YAML, the order of keys in each condition definition below is inconsequential and aligns with the sequential requirements of the underlying VTank Condition, despite `afy` files being agnostic to the order.

Based on the condition type, there will be additional properties that appear subsequent to the `Condition: CONDTYPE` declaration.

### Never
```
- Condition: Never
  Action: Chat
    Message: "%% CONFIGURE CHARACTER LIST BELOW %%""
```

### Always
``` 
- Condition: Always
	Action: Chat
	  Message: "/ub opt set VTank.PatchExpressionEngine true"
```

### Not
```
- Condition: Not
  - Condition: Expr
    Expr: "getvar[NeedsAugs]==1"
```

### All
```
- Condition: All
	Conditions: 
	- Expr: "getvar[IsLeader]==0"
	- ChatMessage: "#action follow"
	- 
```

### Any
```
- Condition: Any
	Conditions: 
	- Expr: "getvar[IsLeader]==0"
	- ChatMatch: "#action follow"
```

### Expr
```
- Condition: Expr
  Expr: "getvar[IsLeader]==0"
```

### ChatMatch
```
- Condition: ChatMatch
  Pattern: "#action follow"
```

### ChatCapture
```
- Condition: ChatCapture
  Pattern: "regex"
  ColorIdList:
  - "503123"
  - "F09E42"
```
Since `ColorIdList` is allowed to be an empty string, the entire key can be ommitted if desired.

### MainSlotsLE
```
- Condition: MainSlotsLE
  Slots: 15
```

### SecsInStateGE
```
- Condition: SecsInStateGE
  Seconds: 300
```

### PSecsInStateGE
```
- Condition: PSecsInStateGE
  Seconds: 600
```

### NavEmpty
```
- Condition: NavEmpty
```

### Death
```
- Condition: Death
```

### VendorOpen
```
- Condition: VendorOpen
```

### VendorClosed
```
- Condition: VendorClosed
```

### ItemCountLE
```
- Condition: ItemCountLE
  Name: "Titan Mana Charge"
  Count: 2
```

### ItemCountGE
```
- Condition: ItemCountGE
  Name: "Token of Valor"
  Count: 1
```

### MobsInDistanceByName
```
- Condition: MobsInDistanceByName
  Name: "Tusker Guard"
  Count: 3
  Distance: 5.0
```
NOTE: this was changed from `MobsInDist_Name`

### MobsInDistanceByPriority
```
- Condition: MobsInDistanceByPriority
  Priority: 2
  Count: 3
  Distance: 5.0
```

### NeedToBuff
```
- Condition: NeedToBuff
```

### NoMobsInRange
```
- Condition: NoMobsInRange
  Distance: 5.0
```

### LandblockE
```
- Condition: LandblockE
  Value: "F682000"
```

### LandcellE
```
- Condition: LandcellE
  Value: "F6820A0"
```

### PortalEnter
```
- Condition: PortalEnter
```

### PortalExit
```
- Condition: PortalExit
```

### SecsOnSpellGE
```
- Condition: SecsOnSpellGE
  SpellId: 60123
  SpellName: "Incantation of Fire Protection Self" (??)
  # 15 minutes
  Seconds: 900
```
`SpellId` and `SpellName` are mutually exclusive. Any `SpellName` used will be resolved to a `SpellId` at `met` generation time. If the spell cannot be found, an error will be produced.


### BurdenPercentGE
```
- Condition: BurdenPercentGE
  Value: 100
```

### DistanceToRouteGE
```
- Condition: DistanceToRouteGE
  Distance: 6.0
```

### MetaVersionChanged
```
- Condition: MetaVersionChanged
```
Special condition that is usable in `afy` metas, and is triggered one-time-only whenever the `Meta.Version` field has changed from the last time it was run. This is accomplished using a `pvar` consisting of the most recently run Meta Version, which is compared against the running `met` MetaVersion on startup. If the last running version is different than the current, `MetaVersionChanged` conditions can be used in `InitState` (or any other states triggered at startup prior to returning to main meta states) and will trigger in this state. 

NOTE: This condition may only occur once in any given `State`, as a `pvar` will be checked/set when the InitState is called for a new version. Until an InitState has triggered its `MetaVersionChanged` rule (if one exists), it is subject to be triggered at any future meta run, regardless of whether all other rules triggered by the `MetaVersionChanged` condition have ALREADY run for the version change in the past.


## Actions

Rules are composed of one `Condition` and one `Action`. The only other place that an `Action` may be defined is within an `All` (Multiple) Action type, in which they appear as list items within the Action.

### None
```
  Action: None
```

### SetState
```
	Action: SetState
	  State: "**SetVars**"
```
or if inside an `All` block:
```
  - SetState: "**SetVars"
```

### Chat
```
  Action: Chat
    Text: "/ub playeroption AllowGive false"
```
or if inside an `All` block:
```
  - Chat: "/ub playeroption AllowGive false"
```

### All
```
  Action: All
  - Action: Expr
    DoExpr: "setvar[Started, 1]"
  - Action: Chat
    Message: "/ub opt set VTank.PatchExpressionEngine true"
  - Action: SetState
    Name: "Start"
```

### EmbedNav
```
  Action: EmbedNav
    Name: "GoToBot"
```
The `EmbedNav` action also has optional parameters to control the resulting nav route data that will be used by VTank as a result of this command, similar to the actual named definition of the nav route referenced in `Name` property:
```
  Action: EmbedNav
    Name: "Hunting"
    Reverse: True
    Transform:
     	Translate: 2.10299 6.11059 0.0000
  	  Rotate: 72
```

### TODO
If possible check whether we can support _both_ a shorthand and expanded version for inlining as list items in an `All` block, e.g. allow for both:
```
  - EmbedNav: "GoToBot"
```
while also supporting...
```
  - EmbedNav:
	    Name: "Hunting"
      Label: "[Hunting]"
	    Reverse: True
	    Transform:
	     	Translate: 2.10299 6.11059 0.0000
	  	  Rotate: 72
```

### CallState
```
  Action: CallState
    State: "NavToMansion"
    ReturnTo: "GoToPortalBot"
```

### Return
```
  Action: Return
```

### Expr
```
  Action: Expr
    Expr: "setvar[Started, 1]"
```
(equivalent of `DoExpr` in original `metaf`)

If inside an `All` block, you can use the shorthand form:
```
  - Expr: "setvar[Started, 1]"
```

### ChatExpr
```
  Action: ChatExpr
  Expr: "chatbox[\/w +getcharstringprop[1]+, Hello world]"
```
or shorthand if nested in an `All` block:
```
  - ChatExpr: "Dsdfsdfasdf"
```

### SetWatchdog
```
  Action: SetWatchdog
    State: "Reload"
    Distance: 5.0
    Seconds: 120
```

### ClearWatchdog
```
  Action: ClearWatchdog
```

### GetOpt
```
  Action: GetOpt
    Name: "ApproachDistance"
    Var: "OldApproachDist"
```

### SetOpt
```
  Action: SetOpt
    Name: "ApproachDistance"
    Value: "getvar[OldApproachDist]"
```

### CreateView
```
  Action: CreateView
    Name: "MyControlRemote"
    Path: "views/remotecontrol.xml"
```
You can also inline your XML data for your View using the `Data` property in lieu of the `Path` property, e.g.
```
  Action: CreateView
    Name: "MyRemoteControl"
    Data: |
    	<?xml version="1.0">
    		...
```

### DestroyView
```
  Action: DestroyView
    Name: "MyRemoteControl"
```
or shorthand if nested in `All`:
```
  - DestroyView: "MyRemoteControl"
```

### DestroyAllViews
```
  Action: DestroyAllViews
```

### ImportFragment
```
  Action: ImportFragment
    Name: CheckStuck
    Vars:
    - Name: ReturnToState
      Value: "NavTo"
```

### ClearFragmentVars
```
  Action: ClearFragmentVars
```

### SetManagedVars
Sets one or more variables to 
```
  Action: SetManagedVars
    Vars:
    - Name: GetAug
      Value: "1"
    - Name: NavTo_Started
      Value: "1"
    - Name: NavTo_BotRequest
      ValueList: 
      # portal bot keyword
      - crater
      # expected landcell (hex)
      - 90D00107
```

### RegisterManagedVars
Registers one or more var names to clear on state transitions, without setting a value.
For use with multiple variable names, you can use the syntax:
```
  Action: RegisterManagedVars
    Names: 
    - GetAug
    - NavTo_Started
        ...
    - NavTo_UsingPortal

```
or for a single var name, you can use the shorthand:
```
  Action: RegisterManagedVars
    Name: NavTo_Started
```


### ClearManagedVars
Clears all variables that were set for the current state (by name) based on all occurrences of the `SetManagedVars` rules for that state.
```
  Action: ClearManagedVars
```


# Custom Value Types
Due to the flexibility of YAML, `afy` files are able to use custom value types that do not map directly to VTank Meta constructs directly, and some of which may dependencies on additional plugins, specifically UtilityBelt.

* ValueRef provides the ability to reference a variable in the Go template representing the meta (top-level), the meta state being defined, or input parameters for the generation command if the declaring file itself is templatable.
```
ValueRef: NAME
```

* ValueList provides the ability to inline a list of values, requiring the `--utilitybelt` parameter to be provided when generating a `met` for this `afy` file. Usage of this directive translates to a `listcreate[... items]` UtilityBelt expression call.
```
ValueList: 
- "first"
- "second"
- "third"
```

# Fragment Templating
Meta and State Fragments defined in their own `afyf` files have slightly different formats than the `afy` format. Namely, they allow the use of Go Templating using the double open and closing curly braces. See Go Templating references at beginning of doc for additional information on availability of features beyond mere variable substitution, including but not limited to conditionals and looping.

## Common properties of Meta and State Fragments

### Optional Fragment Definition Properties

#### NamePrefix
The `NamePrefix` property can be declared within either a `MetaFragment` or a `StateFragment` and is used to prefix both variables and state names, unless otherwise declared in the property for that specific prefix (see below)

#### StateNamePrefix
_Meta_ Fragments may declare an optional `StateNamePrefix` which will be prepended to every state imported from the fragment, regardless of the value for `NamePrefix` (if provided)

#### NavRoutePrefix
Both State Fragments and Meta Fragments may define their own Nav Routes. To avoid name collisions, while allowing the fragment to be written with human readable names for nav route references, a `NavRoutePrefix` can be provided. If provided, any `NavRoutes` will have their `Name` prepended with said prefix. If this property is not defined, the `NamePrefix` is used, or the `Name` is used as-is if neither is provided.

#### VarNamePrefix
Both Meta Fragments and State Fragments may declare an optional `VarNamePrefix` which is prepended to every variable used/managed by the fragment, if desired.

_NOTE_: It is the responsibility of the Fragment code itself to respect the prefix properties when provided to the fragment. If this were automatic, it would introduce unnecessary complexity by forcing expression parsing and may prevent the ability to use both prefixes and non-prefixed vars within fragments.

## Fragments requiring one-time initialization
It is typical for groups of states encapsulating functionality in a meta to require some form of statically defined variables in order to operate. Since a Meta Fragment has no awareness of the actual meta it is being imported into, there needs to be some way to support a fragment initializing its own variables within the context of this limitation. In order to achieve this Meta Fragments provide the option for a property to register a state handling initialization for the fragment with the `Meta`:
```
  FragmentInitState: "InitLandscapeObjects"
```
This allows a meta fragment to register its own state that will be called in sequence during meta initialization. This allows for state fragments to set vars or pvars that can be used by their other states, without the importing meta needing to have any awareness. 

When the `InitState` (plural!) property is provided in the `Meta` definition, a synthetic state is created that iterates over all registered `InitStates`. The top-level `Meta` definition is able to provide an initial list of `InitStates` to call at startup, while all `RegisterInitState` found in declared `Fragments` in the `Meta` will be added to `InitStates` as well, since parsing of Meta Fragments is done prior to generation of a `met` file.

### Meta Fragment Syntax
Meta Fragments are virtually identical to the standard `afy` format, but with a few exceptions:
* Meta Fragments may use Go Templating syntax (e.g. double open/close curly braces w/"code" inside)
* Meta Fragments are able to use the `RegisterInitState` `Action` (provided the `InitState` name is defined in the meta that is importing the fragment)


### Provided template variables:
All fragments have the following variables available:
#### `.MetaName` is always provided with the simple name of the `met` file being generated (excluding extension)
#### `.ResetState` is provided with the name of the state that should be used when a hard reset of the meta is required. This value defaults to `Default` state if none is provided in the `Meta` definition (top-level)

### Special Fragment-specific `Action` types:
#### ResetMeta
Special action that resolves to a `SetState` to `Default` OR an explicitly declared `ResetState` from the parent `Meta` definition:
```
  Action: ResetMeta
```
which translates to:
```
  Action: SetState
    State: "{{ .Meta.ResetState }}"
```

## Meta Fragments
Meta Fragments are defined at the top-level, alongside State Fragments, but do not get referenced within any States/Rules within the `Meta` itself. Instead, they are imported as zero or more complete States which have all of their rules automatically added to the importing meta. In the event there may be conflicts with State Names, the `StateNamePrefix` property is available to be prepended to every imported state.

Example syntax:
```
MetaFragment:
  Name: "LandscapeObjects"
  FragmentInitState: "InitLandscapeObjects"
  StateNamePrefix: "LandscapeObjects_"
  # complete states that will be rendered in the parent meta/afy
  States:
  # the rendered state name will be prefixed with StateNamePrefix if defined, e.g. this will become 'LandscapeObjects_ScanNearby'
  - Name: "ScanNearby"
    Rules: 
    - Condition: ... 
      Action: ...
  	- ...
  	- ...

  # the rendered state name will be prefixed with StateNamePrefix if defined, e.g. this will become 'LandscapeObjects_CheckBlacklist'
	- Name: "CheckBlacklist"
	    ...
  # the rendered state name will be prefixed with StateNamePrefix if defined, e.g. this will become 'LandscapeObjects_UseObject'
  - Name: "UseObject"
      ...
  # optional list of state fragments that are part of this meta fragment
  StateFragmentSections:
  - Name: ScanIfNeeded
    Rules:
    - Condition: Expr
      Expr: "testvar[LandscapeObjects_ScanStopwatch]==0"
      Action: All
      - Action: SetManagedVars
          Vars:
      - Action: Expr
          Expr: "setvar[LandscapeObjects_ScanStopwatch, stopwatchstart[stopwatchcreate[]]]"
    - Condition: All
      - Condition: Expr
        Expr: "testvar[LandscapeObjects_ScanStopwatch]"
      - Condition: Expr
        Expr: "stopwatchelapsedseconds[$LandscapeObjects_ScanStopwatch]>=$LandscapeObjects_ScanIntervalSecs"
      Action: SetState
        # if this state is defined in this meta fragment, then it will be prepended with StateNamePrefix!
        State: ScanNearby
    # optional list of vars to clear on state transitions
    #   if you use a SetManagedVars action, this is merged into the list of vars cleared on state transitions
    ClearVarsOnChange: 
    - LandscapeObjects_scan_index
    - LandscapeObjects_NearbyObj
    - LandscapeObjects_NavStopwatch
        ...
    - LandscapeObjects_UseStopwatch

  NavRoutes:
  - Name: "NavToPricklyPearNPC"
    Path: "navs/landscape_goto_pricklypear_npc.nav"
  - Name: "EmptyNav"
    	...
```

## State Fragments

As mentioned above, State Fragments can be defined with `DefaultVars`, who values are overridden whenever explicitly provided in the `ImportFragment` statement in a meta. From the perspective of the template, all variables, regardless of the origin of their value, are provided within the `.Vars.NAME` key. Additional provided values are available in different keys in the fragment depending on the fragment type.

### Template variables available exclusively for State Fragments:
* `.StateName` is provided with the name of the state that is importing this fragment
* `.SectionName` is provided with the name of the section being rendered within the state fragment
* `.ClearVarsOnChange` is always provided with a list of var names that should be cleared on any state transition within any rules being rendered for the fragment -- this is used automatically when the fragment leverages the `ClearVarsForState` `Action` within a fragment.
* `.ClearPVarsOnChange` is always provided with a list of pvar names that should be cleared on any state transition within any rules being rendered for the fragment -- this is used automatically when the fragment leverages the `ClearVarsForState` `Action` within a fragment.

### Special `Action` types available only within a State Fragment:

#### ClearVarsForState
This is a Helper Action that is expanded into zero or more actions that will clear every var and pvar that were provided to the template in its `.ClearVarsOnChange` and `.ClearPVarsOnChange` properties, if provided. This action can be used regardless of whether said keys are provided. In the event none are defined, it will simply result in zero Rules being added to the state
```
  Action: ClearVarsForState
```

### State Fragment Syntax
Example syntax:
```
StateFragment:
  Name: "MuleCheck"
  FragmentInitState: "InitMuleList"
  NavRoutes:
  - Name: "NavToMuleFromDrop"
    Path: "navs/hometown_to_mules.nav"
  States:
  # the rendered state name will be prefixed with StateNamePrefix if defined, e.g. this will become 'MuleCheck_GoToMules'
  - Name: GoToMules
    Rules: 
        ...
  Sections:
  - Name: ShouldMule
    Rules: 
    - Condition: MainSlotsLE
        Slots: 12
      Action: SetState
        State: GoToMules
  - Name: ...

```



# Sample `afy` file:
```
# Inline comments are standard YAML format
# blah blah blah

# empty lines are fine too
#
Meta:
  # tabs are usually 2 spaces, generally do NOT use a 'real' tab (\t) but it should be supported by yaml parsers
  Name: "AugGem"
  Metadata:
  	Version: "0.0.1"
  	Author: "Alastor"
  # optional state name to use whenever the meta needs to be 'reset"' -- default is "Default"
	# ResetState: "Reload"
	# optional initialization step that MUST be provided if the RegisterInitState is used anywhere inside any imported meta fragment
	InitState: "**SetVars**"
	# optional predefined list of init states to call (can have additional states registered later)
	InitStates:
	- "InitQuests"
	- "InitChecks"
	- "CheckCharSkills"
	Imports:
		MetaFragments:
		# afyf = afy fragment? or afmf = af meta fragment? (since meta and state fragments are different)
		- Path: "nav/stuck_detection.afyf" 
		  Vars:
		  - Name: "ReturnMetaName"
		    ValueRef: ".Meta.Name"
	    - Name: "NonInterruptibleLandblocks"
	      ValueList: 
	      - "012D0000"
	      - "F6820000"
	      - "D6900000"
		  - Name: "UseAphusRecall"
		    Value: "0"
	    StateNamePrefix: "_sd_"
		StateFragments:
		- Name: "checkstuck"
			# afyf fragment
		  Path: "nav/stuck_detection_state.afyf"
		  Sections:
		  - "checktimer"
		  - "distcheck"
  		DefaultVars:
  		# arbitrary key-value defaults used if no 'vars' entry is provided when importing in states
  		- Name: "imported"
  		  Value: "1"
  		# optional list of variables that must be cleared if the fragment at any point decides a state transition needs to occur
  		# and the state that is importing this fragment will not have the opportunity to clear those managed vars on its own
		  ClearVarsOnStateChange:
		  - "NavActive"
		  - "ReadyAtAphus"
		  - "ReadyAtHometown"
		  - "ReadyAtMansion"
  NavRoutes:
	- Name: "GoToBot"
	  Path: "navs/GoToPortalBot.nav"
  - Name: "EmptyNav"
    Data: |
    	uTank2 NAV 1.2
			4
			0
			|
			|
			|
			blahg
			blah
		|					<== syntax error
	- Name: "FollowLeader"
	  Type: Follow
	  Target: "Alastrius"
	- Name: "SomeNav"
	  Type: Linear|Circular|Once
		Points:
		- Point: 0.05, 65.4, 0.09
		- Chat: hello world!
  States:
  - Name: "NavTo"
		Rules:
		- Condition: All
			- Condition: Expr
				Value: "getvar[GetAug]==1"
			- Condition: NavEmpty
			  Label: "NavEmptyRule"
   	  Action: Chat
   	    Think: "Getting my aug gem!"
    - Condition: Expr
    	  Value: "getvar[NavActive]==0"
    	Action: All
    	- Action: Chat
    		Message: "May it be known... Here I go!"
  		- Action: Expr
  			Value: "setvar[NavActive, 1]"
			- Action: SetOpt
				Name: "EnableBuffing"
				Value: true # ?? or int?
			- Action: SetOpt
				Name: "EnableCombat"
				Value: false
			- Action: SetOpt
				Name: "EnableNav"
				Value: true
		- Do: ImportFragment
				Name: "checkstuck"
			  Section: "distcheck"
			  Vars:
			  # use in a go template loop inside the imported fragment section
			  - Name: "ReturnToStateIfAnyTrue"
			  	ValueList:
			  	- "GetAug"
			  	- "GetJaw"
			  	- "GetStipend"
		- Condition: All
			- Condition: NavEmpty
			- Condition: BlockE
			  Value: F6820000
		  - Condition: Expr
		    Value: "getvar[GetAug]==1"
		  Action: All
		  - Action: Expr
		  	Value: "clearvar[NavActive]"
	  	- Action: Expr
	  		Value: "setvar[ReadyAtAphus, 1]"
			- Action: EmbedNav
  		  Name: "EmptyNav"
  		- Action: SetState
  			Name: "GetAug"
		- Condition: All
			- Condition: NavEmpty
			- Condition: BlockE
			  Value: F6820000
		  - Condition: Expr
		    Value: "getvar[GetStipend]==1"
		  Action: All
		  - Action: Expr
		  	Value: "clearvar[NavActive]"
	  	- Action: Expr
	  		Value: "clearvar[ReadyAtAphus]"
			- Action: EmbedNav
  		  Name: "EmptyNav"
  		- Action: SetState
  			Name: "Stipend"
  	- Do: ImportFragment
  			Name: "checkstuck"
  			Section: "checktimer"
```


void WriteAFNodeWithComments(AFNode node, TextWriter textWriter, YamlWriter yamlWriter) {
	if (node.Comments.Length > 0) {
		foreach (str line in node.Comments) {
			textWriter.WriteLine(indent("#", " ", yamlWriter.indent);
		}
	}
	if (node.Children.Length > 0) {
		yamlWriter.BeginNode(xxx);
		foreach (var child in node.Children) {
			WriteAFNodeWithComments(child, textWriter, yamlWriter);
		}
		yamlWriter.EndNode(xxx);
	} else {
		yamlWriter.Write(node.ToYaml());
	}
	if (node.Children.Length > 0) {
		yamlWriter.EndNode(xxx);
	}
}