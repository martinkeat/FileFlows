using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Service for communicating with FileFlows server for variables
/// </summary>
public class VariableService : CachedService<Variable>, IVariableService
{
}