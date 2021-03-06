// ----------------------------------------------------------------------------------------------------
// <copyright file="GlobalSuppressions.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Microsoft.Design", "CA2210:AssembliesShouldHaveValidStrongNames", Justification = "Not needed for now.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "besharp", Justification = "Library name")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "besharp", Justification = "Library name")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Multi", Scope = "type", Target = "BESharp.Datagrams.CommandMultiPartResponseDatagram", Justification = "WTH")]

[assembly: SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Login", Scope = "member", Target = "BESharp.Datagrams.Constants.#LoginReturnCodeIndex", Justification = "Not really")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Login", Scope = "member", Target = "BESharp.Datagrams.DatagramType.#Login", Justification = "Not really")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Login", Scope = "type", Target = "BESharp.Datagrams.LoginDatagram", Justification = "Not really")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Login", Scope = "type", Target = "BESharp.Datagrams.LoginResponseDatagram", Justification = "Not really")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "BESharp.RConClient.#CreateUdpClient()")]
[assembly: SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Scope = "member", Target = "BESharp.RConClient.#CreateUdpClient()")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "BESharp.RConClient.#StartListening()", Justification = "Wrong CA IL decompile")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "BESharp.KeepAliveTracker.#Log", Justification = "It's used for TRACING")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "BESharp.DatagramSender.#Log", Justification = "It's used for TRACING")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "BESharp.RConClient.#Log", Justification = "It's used for TRACING")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Scope = "member", Target = "BESharp.InboundProcessor.#log", Justification = "It's used for TRACING")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Scope = "member", Target = "BESharp.ResponseHandler.#log", Justification = "It's used for TRACING")]

