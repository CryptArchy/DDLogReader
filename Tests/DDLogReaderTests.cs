using System;
using Xunit;
using DDLogReader;

namespace Tests {
    public class DDLogReaderTests {
        [Fact]
        public void TestLogLineParser() {
            var testLine = "127.0.0.1 - james [09/May/2018:16:00:39 +0000] \"GET /report HTTP/1.0\" 200 1234";
            var ll = new LogLine(testLine);
            Assert.Equal("127.0.0.1", ll.Client);
            Assert.Equal("james", ll.User);
            Assert.Equal(DateTimeOffset.Parse("2018-05-09T16:00:39Z"), ll.Occurance);
            Assert.Equal("GET", ll.Method);
            Assert.Equal("/report", ll.Path);
            Assert.Equal(200, ll.StatusCode);
            Assert.Equal(1234L, ll.Size);
        }
    }
}
