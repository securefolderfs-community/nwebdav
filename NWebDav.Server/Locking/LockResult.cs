using System.Net;

namespace NWebDav.Server.Locking
{
    public readonly record struct LockResult(HttpStatusCode Result, ActiveLock? Lock = null);
}
