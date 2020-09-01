// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "<Pending>", Scope = "member", Target = "~F:TinyMessenger.TinyMessageBase._Sender")]
[assembly: SuppressMessage("Style", "IDE0031:Use null propagation", Justification = "<Pending>", Scope = "member", Target = "~P:TinyMessenger.TinyMessageBase.Sender")]
[assembly: SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "<Pending>", Scope = "member", Target = "~F:TinyMessenger.TinyMessageSubscriptionToken._MessageType")]
[assembly: SuppressMessage("Style", "IDE0019:Use pattern matching", Justification = "<Pending>", Scope = "member", Target = "~M:TinyMessenger.TinyMessageSubscriptionToken.Dispose")]
[assembly: SuppressMessage("Style", "IDE0016:Use 'throw' expression", Justification = "<Pending>", Scope = "member", Target = "~M:TinyMessenger.CancellableGenericTinyMessage`1.#ctor(System.Object,`0,System.Action)")]
[assembly: SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "<Pending>", Scope = "member", Target = "~F:TinyMessenger.TinyMessageSubscriptionToken._Hub")]
[assembly: SuppressMessage("Style", "IDE0016:Use 'throw' expression", Justification = "<Pending>", Scope = "member", Target = "~M:TinyMessenger.TinyMessengerHub.StrongTinyMessageSubscription`1.#ctor(TinyMessenger.TinyMessageSubscriptionToken,System.Action{`0},System.Func{`0,System.Boolean})")]
[assembly: SuppressMessage("Style", "IDE0016:Use 'throw' expression", Justification = "<Pending>", Scope = "member", Target = "~M:TinyMessenger.TinyMessengerHub.WeakTinyMessageSubscription`1.#ctor(TinyMessenger.TinyMessageSubscriptionToken,System.Action{`0},System.Func{`0,System.Boolean})")]
[assembly: SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Resource is stored, not disposed of.", Scope = "member", Target = "~M:EFSMono.SystemModules.SplitSpritePaintingModule.SplitSpritePainter.LoadWorld(EFSMono.SystemModules.TileProcessorModule.TileProcessorObjects.TileMapList)")]
[assembly: SuppressMessage("Usage", "CA2237:Mark ISerializable types with serializable", Justification = "<Pending>", Scope = "type", Target = "~T:TinyMessenger.TinyMessengerSubscriptionException")]
