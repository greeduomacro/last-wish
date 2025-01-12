using System;
using Server.Network;
using Server.Commands;

namespace Server
{
    public class LightCycle
	{
		public const int DayLevel = 4;
		public const int NightLevel = 16;
		public const int DungeonLevel = 26;
		public const int JailLevel = 9;

		private static int m_LevelOverride = int.MinValue;

		public static int LevelOverride
		{
			get{ return m_LevelOverride; }
			set
			{
				m_LevelOverride = value;

				for ( int i = 0; i < NetState.Instances.Count; ++i )
				{
					NetState ns = NetState.Instances[i];
					Mobile m = ns.Mobile;

					if ( m != null )
						m.CheckLightLevels( false );
				}
			}
		}

		public static void Initialize()
		{
			new LightCycleTimer().Start();
			EventSink.Login += new LoginEventHandler( OnLogin );

			CommandSystem.Register( "GlobalLight", AccessLevel.GameMaster, new CommandEventHandler( Light_OnCommand ) );
		}

		[Usage( "GlobalLight <value>" )]
		[Description( "Sets the current global light level." )]
		private static void Light_OnCommand( CommandEventArgs e )
		{
			if ( e.Length >= 1 )
			{
				LevelOverride = e.GetInt32( 0 );
				e.Mobile.SendMessage( "Global light level override has been changed to {0}.", m_LevelOverride );
			}
			else
			{
				LevelOverride = int.MinValue;
				e.Mobile.SendMessage( "Global light level override has been cleared." );
			}
		}

		public static void OnLogin( LoginEventArgs args )
		{
			Mobile m = args.Mobile;

			m.CheckLightLevels( true );
		}

		public static int ComputeLevelFor( Mobile from )
		{
			if ( m_LevelOverride > int.MinValue )
				return m_LevelOverride;

			int hours, minutes;

			Server.Items.Clock.GetTime( from.Map, from.X, from.Y, out hours, out minutes );

            /* OSI times:
			 * 
			 * Midnight ->  3:59 AM : Night
			 *  4:00 AM -> 11:59 PM : Day
			 * 
			 * Custom times:
			 * 
			 * 10:00 PM ->  4:59 AM : Night
             *  5:00 AM ->  6:59 AM : Scale to day
             *  7:00 AM ->  5:59 PM : Day
             *  6:00 PM ->  8:59 PM : Scale to night
             */

            if (hours < 5)
                return NightLevel;

            if (hours < 7)
                return NightLevel + (((((hours - 5) * 60) + minutes) * (DayLevel - NightLevel)) / 120);

            if (hours < 18)
                return DayLevel;

            if (hours < 21)
                return DayLevel + (((((hours - 18) * 60) + minutes) * (NightLevel - DayLevel)) / 120);

            return NightLevel; // should never be
		}

		private class LightCycleTimer : Timer
		{
			public LightCycleTimer() : base( TimeSpan.FromSeconds( 0 ), TimeSpan.FromSeconds( 5.0 ) )
			{
				Priority = TimerPriority.FiveSeconds;
			}

			protected override void OnTick()
			{
				for ( int i = 0; i < NetState.Instances.Count; ++i )
				{
					NetState ns = NetState.Instances[i];
					Mobile m = ns.Mobile;

					if ( m != null )
						m.CheckLightLevels( false );
				}
			}
		}

		public class NightSightTimer : Timer
		{
			private Mobile m_Owner;

			public NightSightTimer( Mobile owner ) : base( TimeSpan.FromMinutes( Utility.Random( 15, 25 ) ) )
			{
				m_Owner = owner;
				Priority = TimerPriority.OneMinute;
			}

			protected override void OnTick()
			{
				m_Owner.EndAction( typeof( LightCycle ) );
				m_Owner.LightLevel = 0;
				BuffInfo.RemoveBuff( m_Owner, BuffIcon.NightSight );
			}
		}
	}
}
