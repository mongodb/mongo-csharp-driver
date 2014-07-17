using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.WireProtocol.Messages;
using NSubstitute;

namespace MongoDB.Driver.Core.Tests.Helpers
{
    public class ReplyMessageHelper
    {
        public static ReplyMessage<T> BuildSuccess<T>(
            T document,
            long cursorId = 0,
            int requestId = 0,
            int responseTo = 0,
            int startingFrom = 0)
        {
            return new ReplyMessage<T>(
                cursorId: cursorId,
                cursorNotFound: false,
                documents: new [] { document }.ToList(),
                numberReturned: 1,
                queryFailure: false,
                queryFailureDocument: null,
                requestId: requestId,
                responseTo: responseTo,
                serializer: Substitute.For<IBsonSerializer<T>>(),
                startingFrom: startingFrom);
        }

        public static ReplyMessage<T> BuildNoDocumentsReturned<T>(
            long cursorId = 0,
            int requestId = 0,
            int responseTo = 0,
            int startingFrom = 0)
        {
            return new ReplyMessage<T>(
                cursorId: cursorId,
                cursorNotFound: false,
                documents: new List<T>(),
                numberReturned: 0,
                queryFailure: false,
                queryFailureDocument: null,
                requestId: requestId,
                responseTo: responseTo,
                serializer: Substitute.For<IBsonSerializer<T>>(),
                startingFrom: startingFrom);
        }
    }
}