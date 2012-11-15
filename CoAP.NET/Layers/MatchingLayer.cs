﻿/*
 * Copyright (c) 2011-2012, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using System;
using System.Collections.Generic;
using System.Text;
using CoAP.Util;
using CoAP.Log;

namespace CoAP.Layers
{
    /// <summary>
    /// This class matches the request/response pairs using the token option. It must
    /// be below the <see cref="CoAP.Layers.TransferLayer"/>
    /// </summary>
    public class MatchingLayer : UpperLayer
    {
        private static ILogger log = LogManager.GetLogger(typeof(MatchingLayer));
        private HashMap<String, RequestResponsePair> _pairs = new HashMap<String, RequestResponsePair>();

        protected override void DoSendMessage(Message msg)
        {
            if (msg is Request)
                AddOpenRequest((Request)msg);
            SendMessageOverLowerLayer(msg);
        }

        protected override void DoReceiveMessage(Message msg)
        {
            if (msg is Response)
            {
                Response response = (Response)msg;
                RequestResponsePair pair = GetOpenRequest(msg.SequenceKey);
                
			    // check for missing token
                if (pair == null)
                {
                    if (response.Token.Length == 0)
                    {
                        if (log.IsInfoEnabled)
                            log.Info("MatchingLayer - Remote endpoint failed to echo token: " + msg.Key);

                        // TODO try to recover from peerAddress
                    }
                    else
                    {
                        if (log.IsInfoEnabled)
                            log.Info("MatchingLayer - Dropping unexpected response: " + response.SequenceKey);
                    }

                    // let timeout handle the problem
                    return;
                }
                else
                {
                    response.Request = pair.request;
                    pair.request.Response = response;

                    if (log.IsDebugEnabled)
                        log.Debug("MatchingLayer - Matched open request: " + response.SequenceKey);

                    // TODO: ObservingManager.getInstance().isObserving(msg.exchangeKey());
                    if (msg.GetFirstOption(OptionType.Observe) == null)
                        RemoveOpenRequest(response.SequenceKey);
                }
            }

            DeliverMessage(msg);
        }

        private RequestResponsePair AddOpenRequest(Request request)
        {
            RequestResponsePair pair = new RequestResponsePair();
            pair.key = request.SequenceKey;
            pair.request = request;

            if (log.IsDebugEnabled)
                log.Debug("MatchingLayer - Storing open request: " + pair.key);

            // FIXME: buggy fix for block transfer in obvervation, since the first request needs to be kept.
            if (!_pairs.ContainsKey(pair.key))// || !request.IsObserving)
                _pairs[pair.key] = pair;

            return pair;
        }

        private RequestResponsePair GetOpenRequest(String key)
        { 
            return _pairs[key];
        }

        private void RemoveOpenRequest(String key)
        {
            _pairs.Remove(key);

            if (log.IsDebugEnabled)
                log.Debug("MatchingLayer - Cleared open request: " + key);
        }

        class RequestResponsePair
        {
            public String key;
            public Request request;
        }
    }
}
