namespace FileFlowTests.Tests.ScriptTests;

/// <summary>
/// Tests for script parameters to ensure the bindings work correctly
/// </summary>
[TestClass]
public class ScriptParameterTests: ScriptTest
{
    /// <summary>
    /// Tests a bool parameter works
    /// </summary>
    [TestMethod]
    public void BoolParameter()
    {
        string code = @"function Script(boolTrue, boolFalse) {
Logger.ILog('BoolTrue: ' + boolTrue);
Logger.ILog('BoolTrue == true: ' + (boolTrue == true));
Logger.ILog('BoolTrue ?: ' + (boolTrue ? 'true' : 'false'));
Logger.ILog('BoolTrue!!: ' + (!!boolTrue));
Logger.ILog('BoolTrue type: ' + typeof(boolTrue));

Logger.ILog('BoolFalse: ' + boolFalse);
Logger.ILog('BoolFalse == true: ' + (boolFalse == true));
Logger.ILog('BoolFalse ?: ' + (boolFalse ? 'true' : 'false'));
Logger.ILog('BoolFalse!!: ' + (!!boolFalse));
Logger.ILog('BoolFalse type: ' + typeof(boolFalse));
return 0;
}";
        var result = ExecuteScript(code, new()
        {
            { "boolTrue", true },
            { "boolFalse", false }
        });
        Assert.IsTrue(result.Log.Contains("BoolTrue: true"));
        Assert.IsTrue(result.Log.Contains("BoolTrue == true: true"));
        Assert.IsTrue(result.Log.Contains("BoolTrue ?: true"));
        Assert.IsTrue(result.Log.Contains("BoolTrue!!: true"));
        Assert.IsTrue(result.Log.Contains("BoolTrue type: boolean"));
        
        Assert.IsTrue(result.Log.Contains("BoolFalse: false"));
        Assert.IsTrue(result.Log.Contains("BoolFalse == true: false"));
        Assert.IsTrue(result.Log.Contains("BoolFalse ?: false"));
        Assert.IsTrue(result.Log.Contains("BoolFalse!!: false"));
        Assert.IsTrue(result.Log.Contains("BoolFalse type: boolean"));
    }
}