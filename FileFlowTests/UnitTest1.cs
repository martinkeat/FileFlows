namespace FileFlowTests;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public void TestMethod1()
    {
        var node = new ProcessingNode();
        node.Mappings = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("/media/downloads", @"\\tower\downloads\sabnzbd"),
            new KeyValuePair<string, string>("/media/movies", @"\\tower\movies"),
            new KeyValuePair<string, string>("/media/tv", @"\\tower\tv"),
        };
        string path = node.Map("/media/downloads/converted/tv/SomeFolder/SomeFile.mkv");
        Assert.AreEqual(@"\\tower\downloads\sabnzbd\converted\tv\SomeFolder\SomeFile.mkv", path);
    }

    [TestMethod]
    public void WeekNumTest()
    {
        Assert.AreEqual(1, GetWeekNumber(new DateTime(2023, 6, 4)));
        Assert.AreEqual(2, GetWeekNumber(new DateTime(2023, 6, 8)));
        Assert.AreEqual(1, GetWeekNumber(new DateTime(2023, 6, 3)));
        Assert.AreEqual(2, GetWeekNumber(new DateTime(2023, 6, 5)));
        Assert.AreEqual(2, GetWeekNumber(new DateTime(2023, 6, 11)));
        Assert.AreEqual(3, GetWeekNumber(new DateTime(2023, 6, 12)));
        Assert.AreEqual(3, GetWeekNumber(new DateTime(2023, 6, 18)));
        Assert.AreEqual(4, GetWeekNumber(new DateTime(2023, 6, 19)));
        Assert.AreEqual(4, GetWeekNumber(new DateTime(2023, 6, 25)));
        Assert.AreEqual(5, GetWeekNumber(new DateTime(2023, 6, 26)));
        Assert.AreEqual(5, GetWeekNumber(new DateTime(2023, 6, 30)));
    }
    
    static int GetWeekNumber(DateTime date)
    {
        DateTime firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
        DayOfWeek firstDayOfWeek = DayOfWeek.Monday;
    
        // Find the first day of the week that falls on or before the first day of the month
        while (firstDayOfMonth.DayOfWeek != firstDayOfWeek)
        {
            firstDayOfMonth = firstDayOfMonth.AddDays(-1);
        }
    
        int weekNumber = (date.Subtract(firstDayOfMonth).Days / 7) + 1;

        return weekNumber;
    }
}