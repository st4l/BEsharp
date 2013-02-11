// ----------------------------------------------------------------------------------------------------
// <copyright file="IUdpClient.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
using System.Runtime;
using System.Security.Permissions;
namespace BESharp
{
    using System.Net.Sockets;
    using System.Threading.Tasks;


    internal interface IUdpClient
    {
        /// <summary>
        ///   Sends a UDP datagram asynchronously to a remote host.
        /// </summary>
        /// <returns> Returns <see cref="T:System.Threading.Tasks.Task`1" /> . </returns>
        /// <param name="datagram"> An array of type <see cref="T:System.Byte" /> that specifies the UDP datagram that you intend to send represented as an array of bytes. </param>
        /// <param name="bytes"> The number of bytes in the datagram. </param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="datagram" />
        ///   is null.</exception>
        /// <exception cref="T:System.InvalidOperationException">The
        ///   <see cref="T:System.Net.Sockets.UdpClient" />
        ///   has already established a default remote host.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The
        ///   <see cref="T:System.Net.Sockets.UdpClient" />
        ///   is closed.</exception>
        /// <exception cref="T:System.Net.Sockets.SocketException">An error occurred when accessing the socket. See the Remarks section for more information.</exception>
        [HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
        Task<int> SendAsync(byte[] datagram, int bytes);


        /// <summary>
        ///   Sends a UDP datagram to a remote host.
        /// </summary>
        /// <returns> The number of bytes sent. </returns>
        /// <param name="dgram"> An array of type <see cref="T:System.Byte" /> that specifies the UDP datagram that you intend to send represented as an array of bytes. </param>
        /// <param name="bytes"> The number of bytes in the datagram. </param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="dgram" />
        ///   is null.</exception>
        /// <exception cref="T:System.InvalidOperationException">The
        ///   <see cref="T:System.Net.Sockets.UdpClient" />
        ///   has already established a default remote host.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The
        ///   <see cref="T:System.Net.Sockets.UdpClient" />
        ///   is closed.</exception>
        /// <exception cref="T:System.Net.Sockets.SocketException">An error occurred when accessing the socket. See the Remarks section for more information.</exception>
        /// <PermissionSet>
        ///   <IPermission
        ///     class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
        ///     version="1" Unrestricted="true" />
        ///   <IPermission
        ///     class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
        ///     version="1" Unrestricted="true" />
        ///   <IPermission
        ///     class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
        ///     version="1" Flags="UnmanagedCode, ControlEvidence" />
        ///   <IPermission
        ///     class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
        ///     version="1" Unrestricted="true" />
        /// </PermissionSet>
        int Send(byte[] dgram, int bytes);


        /// <summary>
        ///   Returns a UDP datagram asynchronously that was sent by a remote host.
        /// </summary>
        /// <returns> Returns <see cref="T:System.Threading.Tasks.Task`1" /> . </returns>
        /// <exception cref="T:System.ObjectDisposedException">The underlying
        ///   <see cref="T:System.Net.Sockets.Socket" />
        ///   has been closed.</exception>
        /// <exception cref="T:System.Net.Sockets.SocketException">An error occurred when accessing the socket. See the Remarks section for more information.</exception>
        [HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
        Task<UdpReceiveResult> ReceiveAsync();


        /// <summary>
        ///   Closes the UDP connection.
        /// </summary>
        /// <exception cref="T:System.Net.Sockets.SocketException">An error occurred when accessing the socket. See the Remarks section for more information.</exception>
        /// <PermissionSet>
        ///   <IPermission
        ///     class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
        ///     version="1" Unrestricted="true" />
        ///   <IPermission
        ///     class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
        ///     version="1" Unrestricted="true" />
        ///   <IPermission
        ///     class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
        ///     version="1" Flags="UnmanagedCode, ControlEvidence" />
        /// </PermissionSet>
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        void Close();
    }
}
