using System;
using System.IO;
using Xunit;
using DDLogReader;

namespace Tests {
    public class DDLogReaderTests {
        /// <summary>
        /// Tests the LogLine regex parser
        /// </summary>
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

        /// <summary>
        /// Tests the emission of aggregate events from the LogAggregator and the proper calculation of aggregate values.
        /// </summary>
        [Fact]
        public void TestProcessingEvent() {
            var textreader = new StringReader(SampleLogString);
            var logreader = new LogReader(textreader);
            var aggregator = new LogAggregator(logreader, 1, 1000);
            var processed = false;
            aggregator.AggregateProcessed += (sender, agg) => {
                //Console.WriteLine($"LPS: {agg.RollingAverageLPS}, Total: {agg.RollingTotal}");
                Assert.Equal(240, agg.RollingTotal);
                processed = true;
            };
            var cts = new System.Threading.CancellationTokenSource(1100);
            var t = logreader.ReadFile(cts.Token);
            t.Wait(1100, cts.Token);
            Assert.True(processed);
        }

        /// <summary>
        /// Tests the emission of alert and return-to-normal events from the LogAggregator.
        /// </summary>
        [Fact]
        public void TestAlertEvents() {
            var textreader = new StringReader(SampleLogString);
            var logreader = new LogReader(textreader);
            var aggregator = new LogAggregator(logreader, 1, 1000, 2000);
            var alertTriggered = false;
            var alertCancelled = false;
            aggregator.AlertTriggered += (sender, agg) => {
                //Console.WriteLine($"ALERT: LPS: {agg.RollingAverageLPS}, Total: {agg.RollingTotal}");
                alertTriggered = true;
                Assert.Equal(120, agg.RollingAverageLPS);
                Assert.Equal(240, agg.RollingTotal);
            };
            aggregator.AlertCancelled += (sender, agg) => {
                //Console.WriteLine($"CANCEL: LPS: {agg.RollingAverageLPS}, Total: {agg.RollingTotal}");
                alertCancelled = true;
                Assert.Equal(0, agg.RollingAverageLPS);
                Assert.Equal(0, agg.RollingTotal);
            };
            var cts = new System.Threading.CancellationTokenSource(3100);
            var t = logreader.ReadFile(cts.Token);
            t.Wait(3100, cts.Token);
            Assert.True(alertTriggered);
            Assert.True(alertCancelled);
        }

        private const string SampleLogString = @"
127.0.0.1 - mary [01/Oct/2018:18:20:12 +00:00] ""PUT /report HTTP/1.0"" 404 621623234
127.0.0.1 - bailey [01/Oct/2018:18:20:12 +00:00] ""PUT /admin/user HTTP/1.0"" 501 2143373123
127.0.0.1 - avery [01/Oct/2018:18:20:12 +00:00] ""GET /api/comment HTTP/1.0"" 501 1594592069
127.0.0.1 - lane [01/Oct/2018:18:20:12 +00:00] ""PUT /admin/portal HTTP/1.0"" 200 1467226848
127.0.0.1 - carson [01/Oct/2018:18:20:12 +00:00] ""GET /admin/portal HTTP/1.0"" 501 1958835091
127.0.0.1 - bailey [01/Oct/2018:18:20:13 +00:00] ""PATCH /admin/portal HTTP/1.0"" 200 298722049
127.0.0.1 - carson [01/Oct/2018:18:20:13 +00:00] ""GET /api/post HTTP/1.0"" 404 567206974
127.0.0.1 - drew [01/Oct/2018:18:20:13 +00:00] ""GET /api/user HTTP/1.0"" 404 440396024
127.0.0.1 - frank [01/Oct/2018:18:20:13 +00:00] ""PATCH /admin/moderate HTTP/1.0"" 200 2750761
127.0.0.1 - carson [01/Oct/2018:18:20:13 +00:00] ""POST /admin/user HTTP/1.0"" 200 1828513100
127.0.0.1 - frank [01/Oct/2018:18:20:13 +00:00] ""PATCH /admin/portal HTTP/1.0"" 200 393467307
127.0.0.1 - carson [01/Oct/2018:18:20:13 +00:00] ""PUT /admin/moderate HTTP/1.0"" 501 201996919
127.0.0.1 - drew [01/Oct/2018:18:20:13 +00:00] ""POST /report HTTP/1.0"" 200 554934140
127.0.0.1 - kelsey [01/Oct/2018:18:20:14 +00:00] ""GET /admin/moderate HTTP/1.0"" 200 1307732924
127.0.0.1 - bailey [01/Oct/2018:18:20:14 +00:00] ""PUT /admin/user HTTP/1.0"" 404 970018615
127.0.0.1 - drew [01/Oct/2018:18:20:14 +00:00] ""POST /report HTTP/1.0"" 200 858012522
127.0.0.1 - kelsey [01/Oct/2018:18:20:14 +00:00] ""PUT /admin/moderate HTTP/1.0"" 501 1780682628
127.0.0.1 - carson [01/Oct/2018:18:20:14 +00:00] ""POST /admin/portal HTTP/1.0"" 200 990290082
127.0.0.1 - marley [01/Oct/2018:18:20:15 +00:00] ""PUT /api/user HTTP/1.0"" 200 1079937361
127.0.0.1 - jill [01/Oct/2018:18:20:15 +00:00] ""GET /report HTTP/1.0"" 404 1620290554
127.0.0.1 - avery [01/Oct/2018:18:20:15 +00:00] ""POST /admin/moderate HTTP/1.0"" 200 527075350
127.0.0.1 - bailey [01/Oct/2018:18:20:15 +00:00] ""PUT /admin/user HTTP/1.0"" 501 792858180
127.0.0.1 - kelsey [01/Oct/2018:18:20:16 +00:00] ""DELETE /api/post HTTP/1.0"" 200 822957569
127.0.0.1 - carson [01/Oct/2018:18:20:16 +00:00] ""POST /admin/moderate HTTP/1.0"" 200 803768987
127.0.0.1 - carson [01/Oct/2018:18:20:16 +00:00] ""PUT /admin/user HTTP/1.0"" 404 281569882
127.0.0.1 - chris [01/Oct/2018:18:20:16 +00:00] ""PUT /admin/user HTTP/1.0"" 200 945960819
127.0.0.1 - lane [01/Oct/2018:18:20:16 +00:00] ""PUT /admin/moderate HTTP/1.0"" 501 1488034433
127.0.0.1 - lane [01/Oct/2018:18:20:16 +00:00] ""POST /api/user HTTP/1.0"" 404 903964425
127.0.0.1 - chris [01/Oct/2018:18:20:16 +00:00] ""DELETE /admin/user HTTP/1.0"" 501 1583246784
127.0.0.1 - lane [01/Oct/2018:18:20:16 +00:00] ""PUT /admin/moderate HTTP/1.0"" 501 1064919380
127.0.0.1 - james [01/Oct/2018:18:20:17 +00:00] ""GET /api/post HTTP/1.0"" 404 942551095
127.0.0.1 - james [01/Oct/2018:18:20:17 +00:00] ""PATCH /api/comment HTTP/1.0"" 501 697050498
127.0.0.1 - mary [01/Oct/2018:18:20:17 +00:00] ""DELETE /api/comment HTTP/1.0"" 501 269522768
127.0.0.1 - marley [01/Oct/2018:18:20:17 +00:00] ""POST /api/user HTTP/1.0"" 200 362443771
127.0.0.1 - mary [01/Oct/2018:18:20:17 +00:00] ""PUT /admin/portal HTTP/1.0"" 200 533340215
127.0.0.1 - carson [01/Oct/2018:18:20:17 +00:00] ""DELETE /api/user HTTP/1.0"" 200 1482866142
127.0.0.1 - carson [01/Oct/2018:18:20:18 +00:00] ""POST /admin/user HTTP/1.0"" 200 667699623
127.0.0.1 - james [01/Oct/2018:18:20:18 +00:00] ""PUT /admin/portal HTTP/1.0"" 501 813224974
127.0.0.1 - marley [01/Oct/2018:18:20:18 +00:00] ""DELETE /api/post HTTP/1.0"" 501 2114571304
127.0.0.1 - carson [01/Oct/2018:18:20:18 +00:00] ""DELETE /admin/moderate HTTP/1.0"" 200 523933641
127.0.0.1 - mary [01/Oct/2018:18:20:12 +00:00] ""PUT /report HTTP/1.0"" 404 621623234
127.0.0.1 - bailey [01/Oct/2018:18:20:12 +00:00] ""PUT /admin/user HTTP/1.0"" 501 2143373123
127.0.0.1 - avery [01/Oct/2018:18:20:12 +00:00] ""GET /api/comment HTTP/1.0"" 501 1594592069
127.0.0.1 - lane [01/Oct/2018:18:20:12 +00:00] ""PUT /admin/portal HTTP/1.0"" 200 1467226848
127.0.0.1 - carson [01/Oct/2018:18:20:12 +00:00] ""GET /admin/portal HTTP/1.0"" 501 1958835091
127.0.0.1 - bailey [01/Oct/2018:18:20:13 +00:00] ""PATCH /admin/portal HTTP/1.0"" 200 298722049
127.0.0.1 - carson [01/Oct/2018:18:20:13 +00:00] ""GET /api/post HTTP/1.0"" 404 567206974
127.0.0.1 - drew [01/Oct/2018:18:20:13 +00:00] ""GET /api/user HTTP/1.0"" 404 440396024
127.0.0.1 - frank [01/Oct/2018:18:20:13 +00:00] ""PATCH /admin/moderate HTTP/1.0"" 200 2750761
127.0.0.1 - carson [01/Oct/2018:18:20:13 +00:00] ""POST /admin/user HTTP/1.0"" 200 1828513100
127.0.0.1 - frank [01/Oct/2018:18:20:13 +00:00] ""PATCH /admin/portal HTTP/1.0"" 200 393467307
127.0.0.1 - carson [01/Oct/2018:18:20:13 +00:00] ""PUT /admin/moderate HTTP/1.0"" 501 201996919
127.0.0.1 - drew [01/Oct/2018:18:20:13 +00:00] ""POST /report HTTP/1.0"" 200 554934140
127.0.0.1 - kelsey [01/Oct/2018:18:20:14 +00:00] ""GET /admin/moderate HTTP/1.0"" 200 1307732924
127.0.0.1 - bailey [01/Oct/2018:18:20:14 +00:00] ""PUT /admin/user HTTP/1.0"" 404 970018615
127.0.0.1 - drew [01/Oct/2018:18:20:14 +00:00] ""POST /report HTTP/1.0"" 200 858012522
127.0.0.1 - kelsey [01/Oct/2018:18:20:14 +00:00] ""PUT /admin/moderate HTTP/1.0"" 501 1780682628
127.0.0.1 - carson [01/Oct/2018:18:20:14 +00:00] ""POST /admin/portal HTTP/1.0"" 200 990290082
127.0.0.1 - marley [01/Oct/2018:18:20:15 +00:00] ""PUT /api/user HTTP/1.0"" 200 1079937361
127.0.0.1 - jill [01/Oct/2018:18:20:15 +00:00] ""GET /report HTTP/1.0"" 404 1620290554
127.0.0.1 - avery [01/Oct/2018:18:20:15 +00:00] ""POST /admin/moderate HTTP/1.0"" 200 527075350
127.0.0.1 - bailey [01/Oct/2018:18:20:15 +00:00] ""PUT /admin/user HTTP/1.0"" 501 792858180
127.0.0.1 - kelsey [01/Oct/2018:18:20:16 +00:00] ""DELETE /api/post HTTP/1.0"" 200 822957569
127.0.0.1 - carson [01/Oct/2018:18:20:16 +00:00] ""POST /admin/moderate HTTP/1.0"" 200 803768987
127.0.0.1 - carson [01/Oct/2018:18:20:16 +00:00] ""PUT /admin/user HTTP/1.0"" 404 281569882
127.0.0.1 - chris [01/Oct/2018:18:20:16 +00:00] ""PUT /admin/user HTTP/1.0"" 200 945960819
127.0.0.1 - lane [01/Oct/2018:18:20:16 +00:00] ""PUT /admin/moderate HTTP/1.0"" 501 1488034433
127.0.0.1 - lane [01/Oct/2018:18:20:16 +00:00] ""POST /api/user HTTP/1.0"" 404 903964425
127.0.0.1 - chris [01/Oct/2018:18:20:16 +00:00] ""DELETE /admin/user HTTP/1.0"" 501 1583246784
127.0.0.1 - lane [01/Oct/2018:18:20:16 +00:00] ""PUT /admin/moderate HTTP/1.0"" 501 1064919380
127.0.0.1 - james [01/Oct/2018:18:20:17 +00:00] ""GET /api/post HTTP/1.0"" 404 942551095
127.0.0.1 - james [01/Oct/2018:18:20:17 +00:00] ""PATCH /api/comment HTTP/1.0"" 501 697050498
127.0.0.1 - mary [01/Oct/2018:18:20:17 +00:00] ""DELETE /api/comment HTTP/1.0"" 501 269522768
127.0.0.1 - marley [01/Oct/2018:18:20:17 +00:00] ""POST /api/user HTTP/1.0"" 200 362443771
127.0.0.1 - mary [01/Oct/2018:18:20:17 +00:00] ""PUT /admin/portal HTTP/1.0"" 200 533340215
127.0.0.1 - carson [01/Oct/2018:18:20:17 +00:00] ""DELETE /api/user HTTP/1.0"" 200 1482866142
127.0.0.1 - carson [01/Oct/2018:18:20:18 +00:00] ""POST /admin/user HTTP/1.0"" 200 667699623
127.0.0.1 - james [01/Oct/2018:18:20:18 +00:00] ""PUT /admin/portal HTTP/1.0"" 501 813224974
127.0.0.1 - marley [01/Oct/2018:18:20:18 +00:00] ""DELETE /api/post HTTP/1.0"" 501 2114571304
127.0.0.1 - carson [01/Oct/2018:18:20:18 +00:00] ""DELETE /admin/moderate HTTP/1.0"" 200 523933641
127.0.0.1 - mary [01/Oct/2018:18:20:12 +00:00] ""PUT /report HTTP/1.0"" 404 621623234
127.0.0.1 - bailey [01/Oct/2018:18:20:12 +00:00] ""PUT /admin/user HTTP/1.0"" 501 2143373123
127.0.0.1 - avery [01/Oct/2018:18:20:12 +00:00] ""GET /api/comment HTTP/1.0"" 501 1594592069
127.0.0.1 - lane [01/Oct/2018:18:20:12 +00:00] ""PUT /admin/portal HTTP/1.0"" 200 1467226848
127.0.0.1 - carson [01/Oct/2018:18:20:12 +00:00] ""GET /admin/portal HTTP/1.0"" 501 1958835091
127.0.0.1 - bailey [01/Oct/2018:18:20:13 +00:00] ""PATCH /admin/portal HTTP/1.0"" 200 298722049
127.0.0.1 - carson [01/Oct/2018:18:20:13 +00:00] ""GET /api/post HTTP/1.0"" 404 567206974
127.0.0.1 - drew [01/Oct/2018:18:20:13 +00:00] ""GET /api/user HTTP/1.0"" 404 440396024
127.0.0.1 - frank [01/Oct/2018:18:20:13 +00:00] ""PATCH /admin/moderate HTTP/1.0"" 200 2750761
127.0.0.1 - carson [01/Oct/2018:18:20:13 +00:00] ""POST /admin/user HTTP/1.0"" 200 1828513100
127.0.0.1 - frank [01/Oct/2018:18:20:13 +00:00] ""PATCH /admin/portal HTTP/1.0"" 200 393467307
127.0.0.1 - carson [01/Oct/2018:18:20:13 +00:00] ""PUT /admin/moderate HTTP/1.0"" 501 201996919
127.0.0.1 - drew [01/Oct/2018:18:20:13 +00:00] ""POST /report HTTP/1.0"" 200 554934140
127.0.0.1 - kelsey [01/Oct/2018:18:20:14 +00:00] ""GET /admin/moderate HTTP/1.0"" 200 1307732924
127.0.0.1 - bailey [01/Oct/2018:18:20:14 +00:00] ""PUT /admin/user HTTP/1.0"" 404 970018615
127.0.0.1 - drew [01/Oct/2018:18:20:14 +00:00] ""POST /report HTTP/1.0"" 200 858012522
127.0.0.1 - kelsey [01/Oct/2018:18:20:14 +00:00] ""PUT /admin/moderate HTTP/1.0"" 501 1780682628
127.0.0.1 - carson [01/Oct/2018:18:20:14 +00:00] ""POST /admin/portal HTTP/1.0"" 200 990290082
127.0.0.1 - marley [01/Oct/2018:18:20:15 +00:00] ""PUT /api/user HTTP/1.0"" 200 1079937361
127.0.0.1 - jill [01/Oct/2018:18:20:15 +00:00] ""GET /report HTTP/1.0"" 404 1620290554
127.0.0.1 - avery [01/Oct/2018:18:20:15 +00:00] ""POST /admin/moderate HTTP/1.0"" 200 527075350
127.0.0.1 - bailey [01/Oct/2018:18:20:15 +00:00] ""PUT /admin/user HTTP/1.0"" 501 792858180
127.0.0.1 - kelsey [01/Oct/2018:18:20:16 +00:00] ""DELETE /api/post HTTP/1.0"" 200 822957569
127.0.0.1 - carson [01/Oct/2018:18:20:16 +00:00] ""POST /admin/moderate HTTP/1.0"" 200 803768987
127.0.0.1 - carson [01/Oct/2018:18:20:16 +00:00] ""PUT /admin/user HTTP/1.0"" 404 281569882
127.0.0.1 - chris [01/Oct/2018:18:20:16 +00:00] ""PUT /admin/user HTTP/1.0"" 200 945960819
127.0.0.1 - lane [01/Oct/2018:18:20:16 +00:00] ""PUT /admin/moderate HTTP/1.0"" 501 1488034433
127.0.0.1 - lane [01/Oct/2018:18:20:16 +00:00] ""POST /api/user HTTP/1.0"" 404 903964425
127.0.0.1 - chris [01/Oct/2018:18:20:16 +00:00] ""DELETE /admin/user HTTP/1.0"" 501 1583246784
127.0.0.1 - lane [01/Oct/2018:18:20:16 +00:00] ""PUT /admin/moderate HTTP/1.0"" 501 1064919380
127.0.0.1 - james [01/Oct/2018:18:20:17 +00:00] ""GET /api/post HTTP/1.0"" 404 942551095
127.0.0.1 - james [01/Oct/2018:18:20:17 +00:00] ""PATCH /api/comment HTTP/1.0"" 501 697050498
127.0.0.1 - mary [01/Oct/2018:18:20:17 +00:00] ""DELETE /api/comment HTTP/1.0"" 501 269522768
127.0.0.1 - marley [01/Oct/2018:18:20:17 +00:00] ""POST /api/user HTTP/1.0"" 200 362443771
127.0.0.1 - mary [01/Oct/2018:18:20:17 +00:00] ""PUT /admin/portal HTTP/1.0"" 200 533340215
127.0.0.1 - carson [01/Oct/2018:18:20:17 +00:00] ""DELETE /api/user HTTP/1.0"" 200 1482866142
127.0.0.1 - carson [01/Oct/2018:18:20:18 +00:00] ""POST /admin/user HTTP/1.0"" 200 667699623
127.0.0.1 - james [01/Oct/2018:18:20:18 +00:00] ""PUT /admin/portal HTTP/1.0"" 501 813224974
127.0.0.1 - marley [01/Oct/2018:18:20:18 +00:00] ""DELETE /api/post HTTP/1.0"" 501 2114571304
127.0.0.1 - carson [01/Oct/2018:18:20:18 +00:00] ""DELETE /admin/moderate HTTP/1.0"" 200 523933641
127.0.0.1 - mary [01/Oct/2018:18:20:12 +00:00] ""PUT /report HTTP/1.0"" 404 621623234
127.0.0.1 - bailey [01/Oct/2018:18:20:12 +00:00] ""PUT /admin/user HTTP/1.0"" 501 2143373123
127.0.0.1 - avery [01/Oct/2018:18:20:12 +00:00] ""GET /api/comment HTTP/1.0"" 501 1594592069
127.0.0.1 - lane [01/Oct/2018:18:20:12 +00:00] ""PUT /admin/portal HTTP/1.0"" 200 1467226848
127.0.0.1 - carson [01/Oct/2018:18:20:12 +00:00] ""GET /admin/portal HTTP/1.0"" 501 1958835091
127.0.0.1 - bailey [01/Oct/2018:18:20:13 +00:00] ""PATCH /admin/portal HTTP/1.0"" 200 298722049
127.0.0.1 - carson [01/Oct/2018:18:20:13 +00:00] ""GET /api/post HTTP/1.0"" 404 567206974
127.0.0.1 - drew [01/Oct/2018:18:20:13 +00:00] ""GET /api/user HTTP/1.0"" 404 440396024
127.0.0.1 - frank [01/Oct/2018:18:20:13 +00:00] ""PATCH /admin/moderate HTTP/1.0"" 200 2750761
127.0.0.1 - carson [01/Oct/2018:18:20:13 +00:00] ""POST /admin/user HTTP/1.0"" 200 1828513100
127.0.0.1 - frank [01/Oct/2018:18:20:13 +00:00] ""PATCH /admin/portal HTTP/1.0"" 200 393467307
127.0.0.1 - carson [01/Oct/2018:18:20:13 +00:00] ""PUT /admin/moderate HTTP/1.0"" 501 201996919
127.0.0.1 - drew [01/Oct/2018:18:20:13 +00:00] ""POST /report HTTP/1.0"" 200 554934140
127.0.0.1 - kelsey [01/Oct/2018:18:20:14 +00:00] ""GET /admin/moderate HTTP/1.0"" 200 1307732924
127.0.0.1 - bailey [01/Oct/2018:18:20:14 +00:00] ""PUT /admin/user HTTP/1.0"" 404 970018615
127.0.0.1 - drew [01/Oct/2018:18:20:14 +00:00] ""POST /report HTTP/1.0"" 200 858012522
127.0.0.1 - kelsey [01/Oct/2018:18:20:14 +00:00] ""PUT /admin/moderate HTTP/1.0"" 501 1780682628
127.0.0.1 - carson [01/Oct/2018:18:20:14 +00:00] ""POST /admin/portal HTTP/1.0"" 200 990290082
127.0.0.1 - marley [01/Oct/2018:18:20:15 +00:00] ""PUT /api/user HTTP/1.0"" 200 1079937361
127.0.0.1 - jill [01/Oct/2018:18:20:15 +00:00] ""GET /report HTTP/1.0"" 404 1620290554
127.0.0.1 - avery [01/Oct/2018:18:20:15 +00:00] ""POST /admin/moderate HTTP/1.0"" 200 527075350
127.0.0.1 - bailey [01/Oct/2018:18:20:15 +00:00] ""PUT /admin/user HTTP/1.0"" 501 792858180
127.0.0.1 - kelsey [01/Oct/2018:18:20:16 +00:00] ""DELETE /api/post HTTP/1.0"" 200 822957569
127.0.0.1 - carson [01/Oct/2018:18:20:16 +00:00] ""POST /admin/moderate HTTP/1.0"" 200 803768987
127.0.0.1 - carson [01/Oct/2018:18:20:16 +00:00] ""PUT /admin/user HTTP/1.0"" 404 281569882
127.0.0.1 - chris [01/Oct/2018:18:20:16 +00:00] ""PUT /admin/user HTTP/1.0"" 200 945960819
127.0.0.1 - lane [01/Oct/2018:18:20:16 +00:00] ""PUT /admin/moderate HTTP/1.0"" 501 1488034433
127.0.0.1 - lane [01/Oct/2018:18:20:16 +00:00] ""POST /api/user HTTP/1.0"" 404 903964425
127.0.0.1 - chris [01/Oct/2018:18:20:16 +00:00] ""DELETE /admin/user HTTP/1.0"" 501 1583246784
127.0.0.1 - lane [01/Oct/2018:18:20:16 +00:00] ""PUT /admin/moderate HTTP/1.0"" 501 1064919380
127.0.0.1 - james [01/Oct/2018:18:20:17 +00:00] ""GET /api/post HTTP/1.0"" 404 942551095
127.0.0.1 - james [01/Oct/2018:18:20:17 +00:00] ""PATCH /api/comment HTTP/1.0"" 501 697050498
127.0.0.1 - mary [01/Oct/2018:18:20:17 +00:00] ""DELETE /api/comment HTTP/1.0"" 501 269522768
127.0.0.1 - marley [01/Oct/2018:18:20:17 +00:00] ""POST /api/user HTTP/1.0"" 200 362443771
127.0.0.1 - mary [01/Oct/2018:18:20:17 +00:00] ""PUT /admin/portal HTTP/1.0"" 200 533340215
127.0.0.1 - carson [01/Oct/2018:18:20:17 +00:00] ""DELETE /api/user HTTP/1.0"" 200 1482866142
127.0.0.1 - carson [01/Oct/2018:18:20:18 +00:00] ""POST /admin/user HTTP/1.0"" 200 667699623
127.0.0.1 - james [01/Oct/2018:18:20:18 +00:00] ""PUT /admin/portal HTTP/1.0"" 501 813224974
127.0.0.1 - marley [01/Oct/2018:18:20:18 +00:00] ""DELETE /api/post HTTP/1.0"" 501 2114571304
127.0.0.1 - carson [01/Oct/2018:18:20:18 +00:00] ""DELETE /admin/moderate HTTP/1.0"" 200 523933641
127.0.0.1 - mary [01/Oct/2018:18:20:12 +00:00] ""PUT /report HTTP/1.0"" 404 621623234
127.0.0.1 - bailey [01/Oct/2018:18:20:12 +00:00] ""PUT /admin/user HTTP/1.0"" 501 2143373123
127.0.0.1 - avery [01/Oct/2018:18:20:12 +00:00] ""GET /api/comment HTTP/1.0"" 501 1594592069
127.0.0.1 - lane [01/Oct/2018:18:20:12 +00:00] ""PUT /admin/portal HTTP/1.0"" 200 1467226848
127.0.0.1 - carson [01/Oct/2018:18:20:12 +00:00] ""GET /admin/portal HTTP/1.0"" 501 1958835091
127.0.0.1 - bailey [01/Oct/2018:18:20:13 +00:00] ""PATCH /admin/portal HTTP/1.0"" 200 298722049
127.0.0.1 - carson [01/Oct/2018:18:20:13 +00:00] ""GET /api/post HTTP/1.0"" 404 567206974
127.0.0.1 - drew [01/Oct/2018:18:20:13 +00:00] ""GET /api/user HTTP/1.0"" 404 440396024
127.0.0.1 - frank [01/Oct/2018:18:20:13 +00:00] ""PATCH /admin/moderate HTTP/1.0"" 200 2750761
127.0.0.1 - carson [01/Oct/2018:18:20:13 +00:00] ""POST /admin/user HTTP/1.0"" 200 1828513100
127.0.0.1 - frank [01/Oct/2018:18:20:13 +00:00] ""PATCH /admin/portal HTTP/1.0"" 200 393467307
127.0.0.1 - carson [01/Oct/2018:18:20:13 +00:00] ""PUT /admin/moderate HTTP/1.0"" 501 201996919
127.0.0.1 - drew [01/Oct/2018:18:20:13 +00:00] ""POST /report HTTP/1.0"" 200 554934140
127.0.0.1 - kelsey [01/Oct/2018:18:20:14 +00:00] ""GET /admin/moderate HTTP/1.0"" 200 1307732924
127.0.0.1 - bailey [01/Oct/2018:18:20:14 +00:00] ""PUT /admin/user HTTP/1.0"" 404 970018615
127.0.0.1 - drew [01/Oct/2018:18:20:14 +00:00] ""POST /report HTTP/1.0"" 200 858012522
127.0.0.1 - kelsey [01/Oct/2018:18:20:14 +00:00] ""PUT /admin/moderate HTTP/1.0"" 501 1780682628
127.0.0.1 - carson [01/Oct/2018:18:20:14 +00:00] ""POST /admin/portal HTTP/1.0"" 200 990290082
127.0.0.1 - marley [01/Oct/2018:18:20:15 +00:00] ""PUT /api/user HTTP/1.0"" 200 1079937361
127.0.0.1 - jill [01/Oct/2018:18:20:15 +00:00] ""GET /report HTTP/1.0"" 404 1620290554
127.0.0.1 - avery [01/Oct/2018:18:20:15 +00:00] ""POST /admin/moderate HTTP/1.0"" 200 527075350
127.0.0.1 - bailey [01/Oct/2018:18:20:15 +00:00] ""PUT /admin/user HTTP/1.0"" 501 792858180
127.0.0.1 - kelsey [01/Oct/2018:18:20:16 +00:00] ""DELETE /api/post HTTP/1.0"" 200 822957569
127.0.0.1 - carson [01/Oct/2018:18:20:16 +00:00] ""POST /admin/moderate HTTP/1.0"" 200 803768987
127.0.0.1 - carson [01/Oct/2018:18:20:16 +00:00] ""PUT /admin/user HTTP/1.0"" 404 281569882
127.0.0.1 - chris [01/Oct/2018:18:20:16 +00:00] ""PUT /admin/user HTTP/1.0"" 200 945960819
127.0.0.1 - lane [01/Oct/2018:18:20:16 +00:00] ""PUT /admin/moderate HTTP/1.0"" 501 1488034433
127.0.0.1 - lane [01/Oct/2018:18:20:16 +00:00] ""POST /api/user HTTP/1.0"" 404 903964425
127.0.0.1 - chris [01/Oct/2018:18:20:16 +00:00] ""DELETE /admin/user HTTP/1.0"" 501 1583246784
127.0.0.1 - lane [01/Oct/2018:18:20:16 +00:00] ""PUT /admin/moderate HTTP/1.0"" 501 1064919380
127.0.0.1 - james [01/Oct/2018:18:20:17 +00:00] ""GET /api/post HTTP/1.0"" 404 942551095
127.0.0.1 - james [01/Oct/2018:18:20:17 +00:00] ""PATCH /api/comment HTTP/1.0"" 501 697050498
127.0.0.1 - mary [01/Oct/2018:18:20:17 +00:00] ""DELETE /api/comment HTTP/1.0"" 501 269522768
127.0.0.1 - marley [01/Oct/2018:18:20:17 +00:00] ""POST /api/user HTTP/1.0"" 200 362443771
127.0.0.1 - mary [01/Oct/2018:18:20:17 +00:00] ""PUT /admin/portal HTTP/1.0"" 200 533340215
127.0.0.1 - carson [01/Oct/2018:18:20:17 +00:00] ""DELETE /api/user HTTP/1.0"" 200 1482866142
127.0.0.1 - carson [01/Oct/2018:18:20:18 +00:00] ""POST /admin/user HTTP/1.0"" 200 667699623
127.0.0.1 - james [01/Oct/2018:18:20:18 +00:00] ""PUT /admin/portal HTTP/1.0"" 501 813224974
127.0.0.1 - marley [01/Oct/2018:18:20:18 +00:00] ""DELETE /api/post HTTP/1.0"" 501 2114571304
127.0.0.1 - carson [01/Oct/2018:18:20:18 +00:00] ""DELETE /admin/moderate HTTP/1.0"" 200 523933641
127.0.0.1 - mary [01/Oct/2018:18:20:12 +00:00] ""PUT /report HTTP/1.0"" 404 621623234
127.0.0.1 - bailey [01/Oct/2018:18:20:12 +00:00] ""PUT /admin/user HTTP/1.0"" 501 2143373123
127.0.0.1 - avery [01/Oct/2018:18:20:12 +00:00] ""GET /api/comment HTTP/1.0"" 501 1594592069
127.0.0.1 - lane [01/Oct/2018:18:20:12 +00:00] ""PUT /admin/portal HTTP/1.0"" 200 1467226848
127.0.0.1 - carson [01/Oct/2018:18:20:12 +00:00] ""GET /admin/portal HTTP/1.0"" 501 1958835091
127.0.0.1 - bailey [01/Oct/2018:18:20:13 +00:00] ""PATCH /admin/portal HTTP/1.0"" 200 298722049
127.0.0.1 - carson [01/Oct/2018:18:20:13 +00:00] ""GET /api/post HTTP/1.0"" 404 567206974
127.0.0.1 - drew [01/Oct/2018:18:20:13 +00:00] ""GET /api/user HTTP/1.0"" 404 440396024
127.0.0.1 - frank [01/Oct/2018:18:20:13 +00:00] ""PATCH /admin/moderate HTTP/1.0"" 200 2750761
127.0.0.1 - carson [01/Oct/2018:18:20:13 +00:00] ""POST /admin/user HTTP/1.0"" 200 1828513100
127.0.0.1 - frank [01/Oct/2018:18:20:13 +00:00] ""PATCH /admin/portal HTTP/1.0"" 200 393467307
127.0.0.1 - carson [01/Oct/2018:18:20:13 +00:00] ""PUT /admin/moderate HTTP/1.0"" 501 201996919
127.0.0.1 - drew [01/Oct/2018:18:20:13 +00:00] ""POST /report HTTP/1.0"" 200 554934140
127.0.0.1 - kelsey [01/Oct/2018:18:20:14 +00:00] ""GET /admin/moderate HTTP/1.0"" 200 1307732924
127.0.0.1 - bailey [01/Oct/2018:18:20:14 +00:00] ""PUT /admin/user HTTP/1.0"" 404 970018615
127.0.0.1 - drew [01/Oct/2018:18:20:14 +00:00] ""POST /report HTTP/1.0"" 200 858012522
127.0.0.1 - kelsey [01/Oct/2018:18:20:14 +00:00] ""PUT /admin/moderate HTTP/1.0"" 501 1780682628
127.0.0.1 - carson [01/Oct/2018:18:20:14 +00:00] ""POST /admin/portal HTTP/1.0"" 200 990290082
127.0.0.1 - marley [01/Oct/2018:18:20:15 +00:00] ""PUT /api/user HTTP/1.0"" 200 1079937361
127.0.0.1 - jill [01/Oct/2018:18:20:15 +00:00] ""GET /report HTTP/1.0"" 404 1620290554
127.0.0.1 - avery [01/Oct/2018:18:20:15 +00:00] ""POST /admin/moderate HTTP/1.0"" 200 527075350
127.0.0.1 - bailey [01/Oct/2018:18:20:15 +00:00] ""PUT /admin/user HTTP/1.0"" 501 792858180
127.0.0.1 - kelsey [01/Oct/2018:18:20:16 +00:00] ""DELETE /api/post HTTP/1.0"" 200 822957569
127.0.0.1 - carson [01/Oct/2018:18:20:16 +00:00] ""POST /admin/moderate HTTP/1.0"" 200 803768987
127.0.0.1 - carson [01/Oct/2018:18:20:16 +00:00] ""PUT /admin/user HTTP/1.0"" 404 281569882
127.0.0.1 - chris [01/Oct/2018:18:20:16 +00:00] ""PUT /admin/user HTTP/1.0"" 200 945960819
127.0.0.1 - lane [01/Oct/2018:18:20:16 +00:00] ""PUT /admin/moderate HTTP/1.0"" 501 1488034433
127.0.0.1 - lane [01/Oct/2018:18:20:16 +00:00] ""POST /api/user HTTP/1.0"" 404 903964425
127.0.0.1 - chris [01/Oct/2018:18:20:16 +00:00] ""DELETE /admin/user HTTP/1.0"" 501 1583246784
127.0.0.1 - lane [01/Oct/2018:18:20:16 +00:00] ""PUT /admin/moderate HTTP/1.0"" 501 1064919380
127.0.0.1 - james [01/Oct/2018:18:20:17 +00:00] ""GET /api/post HTTP/1.0"" 404 942551095
127.0.0.1 - james [01/Oct/2018:18:20:17 +00:00] ""PATCH /api/comment HTTP/1.0"" 501 697050498
127.0.0.1 - mary [01/Oct/2018:18:20:17 +00:00] ""DELETE /api/comment HTTP/1.0"" 501 269522768
127.0.0.1 - marley [01/Oct/2018:18:20:17 +00:00] ""POST /api/user HTTP/1.0"" 200 362443771
127.0.0.1 - mary [01/Oct/2018:18:20:17 +00:00] ""PUT /admin/portal HTTP/1.0"" 200 533340215
127.0.0.1 - carson [01/Oct/2018:18:20:17 +00:00] ""DELETE /api/user HTTP/1.0"" 200 1482866142
127.0.0.1 - carson [01/Oct/2018:18:20:18 +00:00] ""POST /admin/user HTTP/1.0"" 200 667699623
127.0.0.1 - james [01/Oct/2018:18:20:18 +00:00] ""PUT /admin/portal HTTP/1.0"" 501 813224974
127.0.0.1 - marley [01/Oct/2018:18:20:18 +00:00] ""DELETE /api/post HTTP/1.0"" 501 2114571304
127.0.0.1 - carson [01/Oct/2018:18:20:18 +00:00] ""DELETE /admin/moderate HTTP/1.0"" 200 523933641
";
    }
}
