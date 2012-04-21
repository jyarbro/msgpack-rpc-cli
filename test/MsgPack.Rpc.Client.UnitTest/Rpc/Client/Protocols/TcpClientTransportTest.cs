﻿#region -- License Terms --
//
// MessagePack for CLI
//
// Copyright (C) 2010 FUJIWARA, Yusuke
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//
#endregion -- License Terms --

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace MsgPack.Rpc.Client.Protocols
{

	/// <summary>
	///Tests the Tcp Client Transport 
	/// </summary>
	[TestFixture()]
	public class TcpClientTransportTest
	{
		public const int TimeoutMilliseconds = 3000;

		private static void TestSendReceiveRequest( Action<IPEndPoint> test )
		{
			TestSendReceiveRequest( ( endPoint, _ ) => test( endPoint ) );
		}

		private static void TestSendReceiveRequest( Action<IPEndPoint, TcpClientTransportManager> test )
		{
			using ( var server = MsgPack.Rpc.Server.CallbackServer.Create( ( id, args ) => args ) )
			{
				var ipEndPoint = new IPEndPoint( IPAddress.Loopback, MsgPack.Rpc.Server.CallbackServer.PortNumber );

				using ( var clientTransportManager = new TcpClientTransportManager( new RpcClientConfiguration() ) )
				{
					test( ipEndPoint, clientTransportManager );
				}
			}
		}

		private static void PackRequest( Packer packer, string id )
		{
			packer.PackArrayHeader( 4 );
			packer.Pack( 0 );
			packer.Pack( 1 );
			packer.PackString( "Test" );
			packer.PackArrayHeader( 1 );
			packer.PackString( id );
		}

		private static void AssertResponse( IList<MessagePackObject> result, params string[] ids )
		{
			Assert.That( result, Is.Not.Null );
			Assert.That( result.Count, Is.EqualTo( 4 ) );
			Assert.That( result[ 0 ] == 1, result[ 0 ].ToString() );
			Assert.That( result[ 1 ] == 1, result[ 1 ].ToString() );
			Assert.That( result[ 2 ].IsNil, result[ 2 ].ToString() );
			Assert.That( result[ 3 ].IsArray );
			Assert.That( result[ 3 ].AsList().Count, Is.EqualTo( 1 ) );
			Assert.That( ids.Contains( result[ 3 ].AsList()[ 0 ].ToString() ), "[{0}] contains '{1}'", String.Join( ", ", ids ), result[ 3 ].AsList()[ 0 ].ToString() );
		}

		private static void TestSendReceiveRequestCore( IPEndPoint endPoint, int count, int concurrency )
		{
			using ( var clientTransportManager = new TcpClientTransportManager( new RpcClientConfiguration() ) )
			using ( var connectTask = clientTransportManager.ConnectAsync( endPoint ) )
			{
				if ( !connectTask.Wait( Debugger.IsAttached ? Timeout.Infinite : TimeoutMilliseconds ) )
				{
					throw new TimeoutException();
				}

				using ( var clientTransport = connectTask.Result )
				{
					for ( int i = 0; i < count; i++ )
					{
						using ( var latch = new CountdownEvent( concurrency ) )
						{
							var ids = Enumerable.Range( i * concurrency, concurrency ).ToArray();
							var args = Enumerable.Repeat( 0, concurrency ).Select( _ => Guid.NewGuid().ToString() ).ToArray();
							var idAndArgs = ids.Zip( args, ( id, arg ) => new { MessageId = id, Guid = arg.ToString() } );
							var requestTable = new ConcurrentDictionary<int, string>();
							var responseTable = new ConcurrentDictionary<int, string>();
							var exceptions = new ConcurrentBag<Exception>();

							if ( Task.Factory.ContinueWhenAll(
									idAndArgs.Select(
										idAndArg =>
											Task.Factory.StartNew(
												() =>
												{
													var requestContext = new ClientRequestContext();
													requestTable[ idAndArg.MessageId ] = idAndArg.Guid;
													requestContext.SetRequest(
														idAndArg.MessageId,
														"Dummy",
														( responseContext, exception, completedSynchronously ) =>
														{
															try
															{
																if ( exception != null )
																{
																	exceptions.Add( exception );
																}
																else
																{
																	responseTable[ responseContext.MessageId.Value ] = MessagePackConvert.DecodeStringStrict( responseContext.ResultBuffer.ToArray() );
																}
															}
															finally
															{
																latch.Signal();
															}
														}
													);
													requestContext.ArgumentsPacker.PackArrayHeader( 1 );
													requestContext.ArgumentsPacker.Pack( idAndArg.Guid );

													return requestContext;
												}
											)
									).ToArray(),
									previouses =>
									{
										var contexts = previouses.Select( previous => previous.Result ).ToArray();
										foreach ( var context in contexts )
										{
											clientTransport.Send( context );
										}
									}
								).Wait( Debugger.IsAttached ? Timeout.Infinite : TimeoutMilliseconds )
							)
							{
								throw new TimeoutException( "Send" );
							}

							// receive
							if ( !latch.Wait( Debugger.IsAttached ? Timeout.Infinite : TimeoutMilliseconds ) )
							{
								throw new TimeoutException( "Receive" );
							}

							if ( exceptions.Any() )
							{
								throw new AggregateException( exceptions );
							}

							Assert.That( requestTable.Count, Is.EqualTo( concurrency ) );
							Assert.That( requestTable, Is.EquivalentTo( responseTable ) );
						}
					}
				}
			}
		}

		[Test()]
		public void TestSendReceiveRequest_Once_Ok()
		{
			TestSendReceiveRequest(
				endPoint => TestSendReceiveRequestCore( endPoint, 1, 1 )
			);
		}

		[Test()]
		public void TestSendReceiveRequest_Twice_Ok()
		{
			TestSendReceiveRequest(
				endPoint => TestSendReceiveRequestCore( endPoint, 2, 1 )
			);
		}

		[Test()]
		public void TestSendReceiveRequest_Parallel_Ok()
		{
			TestSendReceiveRequest(
				endPoint => TestSendReceiveRequestCore( endPoint, 1, 2 )
			);
		}

		private static void TestSendNotify( int concurrency, Action<IPEndPoint, CountdownEvent, IProducerConsumerCollection<string>> test )
		{
			var arriveds = new ConcurrentQueue<string>();
			using ( var server =
				MsgPack.Rpc.Server.CallbackServer.Create(
					( id, args ) =>
					{
						arriveds.Enqueue( args[ 0 ].ToString() );
						return MessagePackObject.Nil;
					}
				)
			)
			{
				var ipEndPoint = new IPEndPoint( IPAddress.Loopback, MsgPack.Rpc.Server.CallbackServer.PortNumber );

				using ( var arrivalLatch = new CountdownEvent( concurrency ) )
				{
					test( ipEndPoint, arrivalLatch, arriveds );
				}
			}
		}

		private static void PackNotify( Packer packer, string id )
		{
			packer.PackArrayHeader( 3 );
			packer.Pack( 2 );
			packer.PackString( "Test" );
			packer.PackArrayHeader( 1 );
			packer.PackString( id );
		}

		private static void TestSendNotifyCore( IPEndPoint endPoint, CountdownEvent arrivalLatch, IProducerConsumerCollection<string> arrivedIds, int count )
		{
			using ( var clientTransportManager = new TcpClientTransportManager( new RpcClientConfiguration() ) )
			using ( var connectTask = clientTransportManager.ConnectAsync( endPoint ) )
			{
				if ( !connectTask.Wait( Debugger.IsAttached ? Timeout.Infinite : TimeoutMilliseconds ) )
				{
					throw new TimeoutException();
				}

				using ( var clientTransport = connectTask.Result )
				{
					for ( int i = 0; i < count; i++ )
					{
						if ( arrivalLatch != null )
						{
							arrivalLatch.Reset();
						}

						var args = Enumerable.Repeat( 0, arrivalLatch.InitialCount ).Select( _ => Guid.NewGuid().ToString() ).ToArray();
						var exceptions = new ConcurrentBag<Exception>();

						if ( Task.Factory.ContinueWhenAll(
								args.Select(
									arg =>
										Task.Factory.StartNew(
											() =>
											{
												var requestContext = new ClientRequestContext();
												requestContext.SetNotification(
													"Dummy",
													( exception, completedSynchronously ) =>
													{
														if ( exception != null )
														{
															exceptions.Add( exception );
															arrivalLatch.Signal();
														}
													}
												);
												requestContext.ArgumentsPacker.PackArrayHeader( 1 );
												requestContext.ArgumentsPacker.Pack( arg );

												return requestContext;
											}
										)
								).ToArray(),
								previouses =>
								{
									var contexts = previouses.Select( previous => previous.Result ).ToArray();
									foreach ( var context in contexts )
									{
										clientTransport.Send( context );
									}
								}
							).Wait( Debugger.IsAttached ? Timeout.Infinite : TimeoutMilliseconds )
						)
						{
							throw new TimeoutException( "Send" );
						}

						// receive
						if ( !arrivalLatch.Wait( Debugger.IsAttached ? Timeout.Infinite : TimeoutMilliseconds ) )
						{
							throw new TimeoutException( "Receive" );
						}

						if ( exceptions.Any() )
						{
							throw new AggregateException( exceptions );
						}
					}
				}
			}
		}

		[Test()]
		public void TestSendNotify_Once_Ok()
		{
			TestSendNotify(
				1,
				( endPoint, arrivalLatch,arrivedIds ) => TestSendNotifyCore( endPoint, arrivalLatch, arrivedIds, 1 )
			);
		}

		[Test()]
		public void TestSendNotify_Twice_Ok()
		{
			TestSendNotify(
				1,
				( endPoint,arrivalLatch, arrivedIds ) => TestSendNotifyCore( endPoint, arrivalLatch, arrivedIds, 2 )
			);
		}

		[Test()]
		public void TestSendNotify_Parallel_Ok()
		{
			TestSendNotify(
				2,
				( endPoint, arrivalLatch,arrivedIds ) => TestSendNotifyCore( endPoint, arrivalLatch, arrivedIds, 1 )
			);
		}
	}
}
