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
using NUnit.Framework;

namespace MsgPack.Rpc.Server.Dispatch
{
	partial class ServiceDescriptionTest
	{

		[Test]
		public void TestEquals_NameAreSame_ApplicationAreSame_VersionAreSame_TypeAreSame_True()
		{
			var left = 
				new ServiceDescription( "TestA", () => new object() )
				{
					Application = "AppA",
					Version = 1
				};

			var right =
				new ServiceDescription( "TestA", () => new object() )
				{
						Application = "AppA",
				Version = 1
				};
				
			Assert.That( left.Equals( right ), Is.True );
		}

		[Test]
		public void TestEquals_NameAreSame_ApplicationAreSame_VersionAreDiffer_TypeAreSame_False()
		{
			var left = 
				new ServiceDescription( "TestA", () => new object() )
				{
					Application = "AppA",
					Version = 1
				};

			var right =
				new ServiceDescription( "TestA", () => new object() )
				{
						Application = "AppA",
				Version = 2
				};
				
			Assert.That( left.Equals( right ), Is.False );
		}

		[Test]
		public void TestEquals_NameAreSame_ApplicationAreDiffer_VersionAreSame_TypeAreSame_False()
		{
			var left = 
				new ServiceDescription( "TestA", () => new object() )
				{
					Application = "AppA",
					Version = 1
				};

			var right =
				new ServiceDescription( "TestA", () => new object() )
				{
						Application = "AppB",
				Version = 1
				};
				
			Assert.That( left.Equals( right ), Is.False );
		}

		[Test]
		public void TestEquals_NameAreSame_ApplicationAreDiffer_VersionAreDiffer_TypeAreSame_False()
		{
			var left = 
				new ServiceDescription( "TestA", () => new object() )
				{
					Application = "AppA",
					Version = 1
				};

			var right =
				new ServiceDescription( "TestA", () => new object() )
				{
						Application = "AppB",
				Version = 2
				};
				
			Assert.That( left.Equals( right ), Is.False );
		}

		[Test]
		public void TestEquals_NameAreSame_ApplicationAreNotNullAndNull_VersionAreSame_TypeAreSame_False()
		{
			var left = 
				new ServiceDescription( "TestA", () => new object() )
				{
					Application = "AppA",
					Version = 1
				};

			var right =
				new ServiceDescription( "TestA", () => new object() )
				{
						Version = 1
				};
				
			Assert.That( left.Equals( right ), Is.False );
		}

		[Test]
		public void TestEquals_NameAreSame_ApplicationAreNotNullAndNull_VersionAreDiffer_TypeAreSame_False()
		{
			var left = 
				new ServiceDescription( "TestA", () => new object() )
				{
					Application = "AppA",
					Version = 1
				};

			var right =
				new ServiceDescription( "TestA", () => new object() )
				{
						Version = 2
				};
				
			Assert.That( left.Equals( right ), Is.False );
		}

		[Test]
		public void TestEquals_NameAreDiffer_ApplicationAreSame_VersionAreSame_TypeAreSame_False()
		{
			var left = 
				new ServiceDescription( "TestA", () => new object() )
				{
					Application = "AppA",
					Version = 1
				};

			var right =
				new ServiceDescription( "TestB", () => new object() )
				{
						Application = "AppA",
				Version = 1
				};
				
			Assert.That( left.Equals( right ), Is.False );
		}

		[Test]
		public void TestEquals_NameAreDiffer_ApplicationAreSame_VersionAreDiffer_TypeAreSame_False()
		{
			var left = 
				new ServiceDescription( "TestA", () => new object() )
				{
					Application = "AppA",
					Version = 1
				};

			var right =
				new ServiceDescription( "TestB", () => new object() )
				{
						Application = "AppA",
				Version = 2
				};
				
			Assert.That( left.Equals( right ), Is.False );
		}

		[Test]
		public void TestEquals_NameAreDiffer_ApplicationAreDiffer_VersionAreSame_TypeAreSame_False()
		{
			var left = 
				new ServiceDescription( "TestA", () => new object() )
				{
					Application = "AppA",
					Version = 1
				};

			var right =
				new ServiceDescription( "TestB", () => new object() )
				{
						Application = "AppB",
				Version = 1
				};
				
			Assert.That( left.Equals( right ), Is.False );
		}

		[Test]
		public void TestEquals_NameAreDiffer_ApplicationAreDiffer_VersionAreDiffer_TypeAreSame_False()
		{
			var left = 
				new ServiceDescription( "TestA", () => new object() )
				{
					Application = "AppA",
					Version = 1
				};

			var right =
				new ServiceDescription( "TestB", () => new object() )
				{
						Application = "AppB",
				Version = 2
				};
				
			Assert.That( left.Equals( right ), Is.False );
		}

		[Test]
		public void TestEquals_NameAreDiffer_ApplicationAreNotNullAndNull_VersionAreSame_TypeAreSame_False()
		{
			var left = 
				new ServiceDescription( "TestA", () => new object() )
				{
					Application = "AppA",
					Version = 1
				};

			var right =
				new ServiceDescription( "TestB", () => new object() )
				{
						Version = 1
				};
				
			Assert.That( left.Equals( right ), Is.False );
		}

		[Test]
		public void TestEquals_NameAreDiffer_ApplicationAreNotNullAndNull_VersionAreDiffer_TypeAreSame_False()
		{
			var left = 
				new ServiceDescription( "TestA", () => new object() )
				{
					Application = "AppA",
					Version = 1
				};

			var right =
				new ServiceDescription( "TestB", () => new object() )
				{
						Version = 2
				};
				
			Assert.That( left.Equals( right ), Is.False );
		}

		[Test]
		public void TestEquals_NameAreSame_ApplicationAreNullAndNotNull_VersionAreSame_TypeAreSame_False()
		{
			var left = 
				new ServiceDescription( "TestA", () => new object() )
				{
					Version = 1
				};

			var right =
				new ServiceDescription( "TestA", () => new object() )
				{
						Application = "AppA",
				Version = 1
				};
				
			Assert.That( left.Equals( right ), Is.False );
		}

		[Test]
		public void TestEquals_NameAreSame_ApplicationAreNullAndNotNull_VersionAreDiffer_TypeAreSame_False()
		{
			var left = 
				new ServiceDescription( "TestA", () => new object() )
				{
					Version = 1
				};

			var right =
				new ServiceDescription( "TestA", () => new object() )
				{
						Application = "AppA",
				Version = 2
				};
				
			Assert.That( left.Equals( right ), Is.False );
		}

		[Test]
		public void TestEquals_NameAreSame_ApplicationAreNullAndNull_VersionAreSame_TypeAreSame_True()
		{
			var left = 
				new ServiceDescription( "TestA", () => new object() )
				{
					Version = 1
				};

			var right =
				new ServiceDescription( "TestA", () => new object() )
				{
						Version = 1
				};
				
			Assert.That( left.Equals( right ), Is.True );
		}

		[Test]
		public void TestEquals_NameAreSame_ApplicationAreNullAndNull_VersionAreDiffer_TypeAreSame_False()
		{
			var left = 
				new ServiceDescription( "TestA", () => new object() )
				{
					Version = 1
				};

			var right =
				new ServiceDescription( "TestA", () => new object() )
				{
						Version = 2
				};
				
			Assert.That( left.Equals( right ), Is.False );
		}

		[Test]
		public void TestEquals_NameAreDiffer_ApplicationAreNullAndNotNull_VersionAreSame_TypeAreSame_False()
		{
			var left = 
				new ServiceDescription( "TestA", () => new object() )
				{
					Version = 1
				};

			var right =
				new ServiceDescription( "TestB", () => new object() )
				{
						Application = "AppA",
				Version = 1
				};
				
			Assert.That( left.Equals( right ), Is.False );
		}

		[Test]
		public void TestEquals_NameAreDiffer_ApplicationAreNullAndNotNull_VersionAreDiffer_TypeAreSame_False()
		{
			var left = 
				new ServiceDescription( "TestA", () => new object() )
				{
					Version = 1
				};

			var right =
				new ServiceDescription( "TestB", () => new object() )
				{
						Application = "AppA",
				Version = 2
				};
				
			Assert.That( left.Equals( right ), Is.False );
		}

		[Test]
		public void TestEquals_NameAreDiffer_ApplicationAreNullAndNull_VersionAreSame_TypeAreSame_False()
		{
			var left = 
				new ServiceDescription( "TestA", () => new object() )
				{
					Version = 1
				};

			var right =
				new ServiceDescription( "TestB", () => new object() )
				{
						Version = 1
				};
				
			Assert.That( left.Equals( right ), Is.False );
		}

		[Test]
		public void TestEquals_NameAreDiffer_ApplicationAreNullAndNull_VersionAreDiffer_TypeAreSame_False()
		{
			var left = 
				new ServiceDescription( "TestA", () => new object() )
				{
					Version = 1
				};

			var right =
				new ServiceDescription( "TestB", () => new object() )
				{
						Version = 2
				};
				
			Assert.That( left.Equals( right ), Is.False );
		}
	}
}
