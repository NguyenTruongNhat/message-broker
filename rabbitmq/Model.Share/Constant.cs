namespace Model.Share
{
    public class Constant
    {
        public static class MyRoutingKey
        {
            public static string Fanount = "fanout-routing-key";
            public static string Topic = "topic-routing-key";
            public static string Direct = "direct-routing-key";
            public static string Header = "header-routing-key";
            public static string Emty = "";
        }
        public static class MyExchange
        {
            public static string Fanount = "fanout-exchange";
            public static string Topic = "topic-exchange";
            public static string Direct = "direct-exchange";
            public static string Header = "header-exchange";
            public static string Emty = "";
        }

        public static class MyQueue
        {
            public static string FanoutQueue1 = "fanout-queue-1";
            public static string FanoutQueue2 = "fanout-queue-2";
            public static string DirectQueue1 = "direct-queue-1";
            public static string DirectQueue2 = "direct-queue-2";
            public static string HeaderQueue1 = "header-queue-1";
            public static string HeaderQueue2 = "header-queue-2";
            public static string TopicQueue1 = "topic-queue-1";
            public static string TopicQueue2 = "topic-queue-2";
            public static string Emty = "";
        }

    }
}
