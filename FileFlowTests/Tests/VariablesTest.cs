namespace FileFlowTests.Tests;

/// <summary>
/// Tests for variable replacements
/// </summary>
[TestClass]
public class VariablesTest
{
    /// <summary>
    /// Tests plex variables in folder names are not escaped
    /// </summary>
    [TestMethod]
    public void PlexTests()
    {
        var variables = new Dictionary<string, object>();
        Assert.AreEqual("ShowName (2020) {tmdb-123456}", VariablesHelper.ReplaceVariables("ShowName (2020) {tmdb-123456}", variables, stripMissing: true));
        Assert.AreEqual("ShowName (2020) {tvdb-123456}", VariablesHelper.ReplaceVariables("ShowName (2020) {tvdb-123456}", variables, stripMissing: true));
        Assert.AreEqual("ShowName (2020) {imdb-123456}", VariablesHelper.ReplaceVariables("ShowName (2020) {imdb-123456}", variables, stripMissing: true));
        
        Assert.AreEqual("ShowName (2020) {tmdb-123456} missing", VariablesHelper.ReplaceVariables("ShowName (2020) {tmdb-123456} {missing}", variables, stripMissing: false));
        Assert.AreEqual("ShowName (2020) {tvdb-123456} missing", VariablesHelper.ReplaceVariables("ShowName (2020) {tvdb-123456} {missing}", variables, stripMissing: false));
        Assert.AreEqual("ShowName (2020) {imdb-123456} missing", VariablesHelper.ReplaceVariables("ShowName (2020) {imdb-123456} {missing}", variables, stripMissing: false));
        variables.Add("tmdb-123456", "bobby");
        variables.Add("tvdb-123456", "drake");
        variables.Add("imdb-123456", "iceman");
        Assert.AreEqual("ShowName (2020) bobby", VariablesHelper.ReplaceVariables("ShowName (2020) {tmdb-123456}", variables, stripMissing: true));
        Assert.AreEqual("ShowName (2020) drake", VariablesHelper.ReplaceVariables("ShowName (2020) {tvdb-123456}", variables, stripMissing: true));
        Assert.AreEqual("ShowName (2020) iceman", VariablesHelper.ReplaceVariables("ShowName (2020) {imdb-123456}", variables, stripMissing: true));
    }

    /// <summary>
    /// Tests a variable with odd characters in it
    /// </summary>
    [TestMethod]
    public void NotValidVariable()
    {
        var variables = new Dictionary<string, object>();
        const string testString = "Test {thi$ is n0t a valid-variable{na-me}!}";
        Assert.AreEqual(testString, VariablesHelper.ReplaceVariables(testString, variables, stripMissing: true));
        variables.Add("thi$ is n0t a valid-variable{na-me}!", "odd{repl@ce}ment");
        Assert.AreEqual("Test odd{repl@ce}ment", VariablesHelper.ReplaceVariables(testString, variables, stripMissing: true));
    }
    

    /// <summary>
    /// Tests a variable with odd characters in it
    /// </summary>
    [TestMethod]
    public void Formatters()
    {
        var variables = new Dictionary<string, object>();
        const string name = "This is mixed Casing!";
        variables["value"] = new DateTime(2022, 10, 29, 11, 41, 32, 532);
        Assert.AreEqual("Test 29/10/2022", VariablesHelper.ReplaceVariables("Test {value|dd/MM/yyyy}", variables, stripMissing: true));
        Assert.AreEqual("Test 29-10-2022", VariablesHelper.ReplaceVariables("Test {value|dd-MM-yyyy}", variables, stripMissing: true));
        Assert.AreEqual("Test 29-10-2022 11:41:32.532 AM", VariablesHelper.ReplaceVariables("Test {value|dd-MM-yyyy hh:mm:ss.fff tt}", variables, stripMissing: true));
        Assert.AreEqual("Test 11:41 AM", VariablesHelper.ReplaceVariables("Test {value|time}", variables, stripMissing: true));
        variables["value"] = name;
        Assert.AreEqual("Test " + name.ToUpper(), VariablesHelper.ReplaceVariables("Test {value!}", variables, stripMissing: true));
        variables["value"] = 12;
        Assert.AreEqual("Test 0012", VariablesHelper.ReplaceVariables("Test {value|0000}", variables, stripMissing: true));
        variables["value"] = 645645654;
        Assert.AreEqual("Test 645.65 MB", VariablesHelper.ReplaceVariables("Test {value|size}", variables, stripMissing: true));
        variables["value"] = "this !:\\/ is #%^&*!~@?%$ not a safe name!..";
        Assert.AreEqual("Test this ! is #%^&!~@%$ not a safe name!", VariablesHelper.ReplaceVariables("Test {value|file}", variables, stripMissing: true));
    }
}