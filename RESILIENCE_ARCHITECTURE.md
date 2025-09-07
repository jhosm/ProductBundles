# Plugin Resilience Architecture

## Overview

The ProductBundles system implements a resilience mechanism to protect against misbehaving `IAmAProductBundle` plugins that could hang or crash the entire system. This document explains the architectural decisions, implementation approach, and trade-offs involved.

## The Problem

Plugin systems face a fundamental challenge: how to execute untrusted code safely without compromising the host system. In our case, plugins implement the `IAmAProductBundle` interface with a synchronous `HandleEvent` method:

```csharp
ProductBundleInstance HandleEvent(string eventName, ProductBundleInstance bundleInstance);
```

**Risk Scenarios:**
- Plugin enters infinite loop
- Plugin performs blocking I/O operations
- Plugin throws unhandled exceptions
- Plugin consumes excessive resources

Without protection, a single misbehaving plugin can hang the entire ProductBundles background service, affecting all other plugins and system operations.

## Our Solution: TaskCompletionSource with Polly Timeout

### Architecture Overview

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│ Background      │───▶│ ResilienceManager│───▶│ Plugin.Handle   │
│ Service         │    │ (Polly Timeout)  │    │ Event (Sync)    │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                              │
                              ▼
                       ┌──────────────────┐
                       │ TaskCompletion   │
                       │ Source Pattern   │
                       └──────────────────┘
```

### Implementation Details

The `ResilienceManager` uses a combination of:

1. **Polly ResiliencePipeline** - Provides timeout policy and cancellation
2. **TaskCompletionSource** - Enables manual task completion control
3. **Background Thread Execution** - Isolates plugin execution

```csharp
var result = await _pipeline.ExecuteAsync(async cancellationToken =>
{
    var tcs = new TaskCompletionSource<ProductBundleInstance>();
    
    // Register cancellation to complete the task immediately
    using var registration = cancellationToken.Register(() => 
        tcs.TrySetCanceled(cancellationToken));
    
    // Execute plugin on background thread (fire-and-forget)
    _ = Task.Run(() =>
    {
        try
        {
            var pluginResult = plugin.HandleEvent(eventName, bundleInstance);
            tcs.TrySetResult(pluginResult);
        }
        catch (Exception ex)
        {
            tcs.TrySetException(ex);
        }
    });
    
    return await tcs.Task; // Returns when completed OR cancelled
});
```

### Why TaskCompletionSource?

The key insight is that **you cannot cancel a running synchronous operation** in .NET. Traditional approaches like `Task.Run(action, cancellationToken)` only prevent the task from starting if already cancelled - they don't interrupt running code.

`TaskCompletionSource` solves this by:

1. **Creating a controllable Task** - We can complete it from external code
2. **Immediate cancellation response** - When timeout occurs, we immediately complete the task
3. **Non-blocking system flow** - The main system continues even if plugin thread hangs

### Execution Flow

```
1. Polly starts timeout timer (default: 30 seconds)
2. Plugin executes on background thread
3. Two possible outcomes:
   
   ✅ Plugin completes normally:
   - tcs.TrySetResult(result) completes the task
   - Result returned to caller
   
   ⏰ Timeout occurs:
   - Polly triggers cancellation
   - tcs.TrySetCanceled() completes the task immediately
   - Method returns null
   - Plugin thread may continue running (orphaned)
```

## Alternative Approaches Considered

### 1. AppDomain Isolation ❌

**Approach:** Load plugins in separate AppDomains for isolation.

**Why Rejected:**
- AppDomains are deprecated in .NET Core/.NET 5+
- High overhead and complexity
- Limited cross-platform support
- Serialization requirements for all plugin data

### 2. Process Isolation ❌

**Approach:** Execute plugins in separate processes.

**Why Rejected:**
- Massive overhead (process creation, IPC)
- Complex serialization of plugin data
- Platform-specific implementation challenges
- Overkill for timeout protection

### 3. Thread.Abort() ❌

**Approach:** Use `Thread.Abort()` to forcefully terminate hanging plugins.

**Why Rejected:**
- `Thread.Abort()` is deprecated and dangerous
- Can leave shared state corrupted
- Not available in .NET Core/.NET 5+
- Unpredictable behavior

### 4. Cooperative Cancellation ❌

**Approach:** Require plugins to check `CancellationToken` periodically.

**Why Rejected:**
- Requires changing the `IAmAProductBundle` interface
- Depends on plugin author cooperation
- Doesn't protect against poorly written plugins
- Breaking change for existing plugins

### 5. Direct Task.Run with CancellationToken ❌

**Approach:** `Task.Run(() => plugin.HandleEvent(...), cancellationToken)`

**Why Rejected:**
- CancellationToken only prevents task **starting**, not interrupts running code
- Synchronous operations cannot be cancelled once started
- System still hangs if plugin blocks

## Trade-offs and Limitations

### Accepted Trade-offs

1. **Orphaned Threads:** If a plugin truly hangs, its background thread continues until process termination
2. **Resource Usage:** One thread per hanging plugin (minimal impact)
3. **Delayed Cleanup:** Hanging plugin resources aren't immediately freed

### Why These Are Acceptable

- **System Protection:** Main system flow is never blocked
- **Minimal Impact:** One thread per hanging plugin is negligible
- **Better Alternative:** Much better than entire system hanging
- **Rare Occurrence:** Most plugins complete or throw exceptions eventually

### Benefits Achieved

- **Non-Breaking:** No changes to existing plugin interface
- **Immediate Protection:** Timeout protection works immediately
- **Simple Implementation:** Easy to understand and maintain
- **Configurable:** Timeout values can be adjusted per deployment
- **Comprehensive Logging:** Full visibility into plugin execution and timeouts

## Configuration

```csharp
// Default 30-second timeout
services.AddPluginResilience();

// Custom timeout
services.AddPluginResilience(TimeSpan.FromSeconds(60));

// In ResilienceManager constructor
var manager = new ResilienceManager(logger, TimeSpan.FromMinutes(2));
```

## Monitoring and Observability

The ResilienceManager provides comprehensive logging:

- **Debug:** Plugin execution start/completion
- **Warning:** Timeout events with duration
- **Error:** Plugin failures and exceptions

Example log output:
```
[Debug] Executing plugin 'sample-plugin' HandleEvent for event 'customer.created'
[Warning] Plugin operation timed out after 30 seconds
[Error] Plugin 'sample-plugin' HandleEvent timed out for event 'customer.created'
```

## Future Enhancements

The current implementation provides a solid foundation for additional resilience patterns:

1. **Circuit Breaker:** Temporarily disable repeatedly failing plugins
2. **Retry Policies:** Retry transient failures with exponential backoff
3. **Bulkhead Isolation:** Limit concurrent plugin executions
4. **Rate Limiting:** Throttle plugin execution frequency
5. **Health Checks:** Monitor plugin health and performance metrics

## Conclusion

The TaskCompletionSource approach provides the optimal balance of:
- **Protection:** Prevents system-wide hangs
- **Simplicity:** Easy to implement and understand
- **Compatibility:** No breaking changes to plugin interface
- **Performance:** Minimal overhead for normal operations

While not perfect (orphaned threads), it's the best practical solution for protecting against misbehaving synchronous plugins in .NET, providing immediate system protection with acceptable trade-offs.
