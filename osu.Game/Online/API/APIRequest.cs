﻿//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System;
using osu.Framework.IO.Network;

namespace osu.Game.Online.API
{
    /// <summary>
    /// An API request with a well-defined response type.
    /// </summary>
    /// <typeparam name="T">Type of the response (used for deserialisation).</typeparam>
    internal class APIRequest<T> : APIRequest
    {
        protected override WebRequest CreateWebRequest() => new JsonWebRequest<T>(Uri);

        public APIRequest()
        {
            base.Success += onSuccess;
        }

        private void onSuccess()
        {
            Success?.Invoke((WebRequest as JsonWebRequest<T>).ResponseObject);
        }

        public new event APISuccessHandler<T> Success;
    }

    /// <summary>
    /// AN API request with no specified response type.
    /// </summary>
    public class APIRequest
    {
        /// <summary>
        /// The maximum amount of time before this request will fail.
        /// </summary>
        internal int Timeout = WebRequest.DEFAULT_TIMEOUT;

        protected virtual string Target => string.Empty;

        protected virtual WebRequest CreateWebRequest() => new WebRequest(Uri);

        protected virtual string Uri => $@"{api.Endpoint}/api/v2/{Target}";

        private double remainingTime => Math.Max(0, Timeout - (DateTime.Now.TotalMilliseconds() - (startTime ?? 0)));

        internal bool ExceededTimeout => remainingTime == 0;

        private double? startTime;

        internal double StartTime => startTime ?? -1;

        private APIAccess api;
        protected WebRequest WebRequest;

        public event APISuccessHandler Success;
        public event APIFailureHandler Failure;

        internal void Perform(APIAccess api)
        {
            if (startTime == null)
                startTime = DateTime.Now.TotalMilliseconds();

            this.api = api;

            if (remainingTime <= 0)
                throw new TimeoutException(@"API request timeout hit");

            WebRequest = CreateWebRequest();
            WebRequest.RetryCount = 0;
            WebRequest.Headers[@"Authorization"] = $@"Bearer {api.AccessToken}";

            WebRequest.BlockingPerform();

            //OsuGame.Scheduler.Add(delegate {
                Success?.Invoke();
            //});
        }

        internal void Fail(Exception e)
        {
            WebRequest?.Abort();
            //OsuGame.Scheduler.Add(delegate {
                Failure?.Invoke(e);
            //});
        }
    }

    public delegate void APIFailureHandler(Exception e);
    public delegate void APISuccessHandler();
    public delegate void APISuccessHandler<T>(T content);
}
