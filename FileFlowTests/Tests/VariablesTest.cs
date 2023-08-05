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
    
}