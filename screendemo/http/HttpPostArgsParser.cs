namespace screendemo.http
{
    class HttpPostArgsParser
    {
        public Dictionary<string, string> Arguments = new();
        public HttpPostArgsParser(Stream inputStream)
        {
            string[] str;
            using (var reader = new StreamReader(inputStream))
            {
                str = reader.ReadToEnd().Split('&');
            }

            foreach (string pair in str)
            {
                string[] splitted = pair.Split('=');
                string key = splitted[0];
                string value = splitted[1];
                Arguments.Add(key, value);
            }
        }
    }
}
