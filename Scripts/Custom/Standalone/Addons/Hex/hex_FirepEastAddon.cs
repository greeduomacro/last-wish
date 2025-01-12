////////////////////////////////////////
//                                    //
//   Generated by CEO's YAAAG - V1.2  //
// (Yet Another Arya Addon Generator) //
//                                    //
////////////////////////////////////////

namespace Server.Items
{
    public class hex_FirepEastAddon : BaseAddon
	{
        private static int[,] m_AddOnSimpleComponents = new int[,] {
			  {7138, -1, 0, 4}, {464, -1, -2, 4}, {465, -2, -1, 4}// 1	2	3	
			, {465, -2, 0, 4}, {466, -2, -2, 4}, {472, -1, -1, 0}// 4	5	6	
			, {490, -1, -1, 21}, {490, -1, 0, 21}, {490, -1, -1, 18}// 7	8	9	
			, {490, -1, 0, 18}, {1313, -1, 0, 4}, {1313, -1, -1, 4}// 10	11	12	
			, {1312, -1, -2, 4}, {464, -1, 1, 4}, {465, -2, 1, 4}// 13	16	17	
			, {490, -1, 1, 21}, {490, -1, 1, 18}, {486, -1, 2, 4}// 18	19	20	
			, {1313, -1, 1, 4}, {1312, -1, 2, 4}, {7712, 2, 0, 4}// 21	22	23	
			, {4604, 1, 0, 4}, {2866, 1, -1, 4}, {7738, 1, 0, 4}// 24	25	26	
			, {7739, 2, 0, 4}, {7740, 2, -1, 4}, {7741, 1, -1, 4}// 27	28	29	
			, {487, 0, -2, 4}, {1312, 0, -2, 4}, {1312, 0, -1, 4}// 30	31	32	
			, {1312, 0, 0, 4}, {7784, 0, 0, 9}, {7737, 0, 0, 4}// 33	34	35	
			, {7742, 0, -1, 4}, {7734, 2, 1, 4}, {7735, 1, 1, 4}// 36	37	38	
			, {487, 0, 1, 4}, {1312, 0, 1, 4}, {1312, 0, 2, 4}// 39	40	41	
			, {7736, 0, 1, 4}// 42	
		};

 
            
		public override BaseAddonDeed Deed
		{
			get
			{
				return new hex_FirepEastAddonDeed();
			}
		}

		[ Constructable ]
		public hex_FirepEastAddon()
		{

            for (int i = 0; i < m_AddOnSimpleComponents.Length / 4; i++)
                AddComponent( new AddonComponent( m_AddOnSimpleComponents[i,0] ), m_AddOnSimpleComponents[i,1], m_AddOnSimpleComponents[i,2], m_AddOnSimpleComponents[i,3] );


			AddComplexComponent( (BaseAddon) this, 14732, -1, 0, 5, 0, 1, "", 1);// 14
			AddComplexComponent( (BaseAddon) this, 14742, -1, 0, 5, 0, 1, "", 1);// 15

		}

		public hex_FirepEastAddon( Serial serial ) : base( serial )
		{
		}

        private static void AddComplexComponent(BaseAddon addon, int item, int xoffset, int yoffset, int zoffset, int hue, int lightsource)
        {
            AddComplexComponent(addon, item, xoffset, yoffset, zoffset, hue, lightsource, null, 1);
        }

        private static void AddComplexComponent(BaseAddon addon, int item, int xoffset, int yoffset, int zoffset, int hue, int lightsource, string name, int amount)
        {
            AddonComponent ac;
            ac = new AddonComponent(item);
            if (name != null && name.Length > 0)
                ac.Name = name;
            if (hue != 0)
                ac.Hue = hue;
            if (amount > 1)
            {
                ac.Stackable = true;
                ac.Amount = amount;
            }
            if (lightsource != -1)
                ac.Light = (LightType) lightsource;
            addon.AddComponent(ac, xoffset, yoffset, zoffset);
        }

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( 0 ); // Version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}

	public class hex_FirepEastAddonDeed : BaseAddonDeed
	{
		public override BaseAddon Addon
		{
			get
			{
				return new hex_FirepEastAddon();
			}
		}

		[Constructable]
		public hex_FirepEastAddonDeed()
		{
			Name = "Fireplace East";
		}

		public hex_FirepEastAddonDeed( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( 0 ); // Version
		}

		public override void	Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
}