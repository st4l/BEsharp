// ----------------------------------------------------------------------------------------------------
// <copyright file="GlobalSuppressions.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Microsoft.Design", "CA2210:AssembliesShouldHaveValidStrongNames", Justification = "Not needed for now.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "besharp", Justification = "Library name")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "besharp", Justification = "Library name")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Multi", Scope = "type", Target = "BESharp.Datagrams.CommandMultiPacketResponseDatagram", Justification = "WTH")]

[assembly: SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Login", Scope = "member", Target = "BESharp.Datagrams.Constants.#LoginReturnCodeIndex", Justification = "Not really")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Login", Scope = "member", Target = "BESharp.Datagrams.DatagramType.#Login", Justification = "Not really")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Login", Scope = "type", Target = "BESharp.Datagrams.LoginDatagram", Justification = "Not really")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Login", Scope = "type", Target = "BESharp.Datagrams.LoginResponseDatagram", Justification = "Not really")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "BESharp.RConClient.#.ctor(System.String,System.Int32,System.String)", Justification = "Wrong CA IL decompile")]
[assembly: SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Scope = "member", Target = "BESharp.RConClient.#.ctor(System.String,System.Int32,System.String)", Justification = "Wrong CA IL decompile")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "BESharp.RConClient.#StartListening()", Justification = "Wrong CA IL decompile")]
