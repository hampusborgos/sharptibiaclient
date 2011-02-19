using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CTC
{
    public class GameImage
    {
        public int ID;
        private TibiaGameData GameData;
        private Byte[] Dump;
        public Texture2D Texture;

        public GameImage(TibiaGameData GameData, int ID)
        {
            this.GameData = GameData;
            this.ID = ID;
        }

        public Byte[] LoadRGBA()
        {
	        if (Dump == null)
			    Dump = GameData.LoadSpriteDump(ID);

            Byte[] rgba32x32 = new Byte[32*32*4];
	        /* SPR dump format
	         *  The spr format contains chunks, a chunk can either be transparent or contain pixel data.
	         *  First 2 bytes (unsigned short) are read, which tells us how long the chunk is. One
	         * chunk can stretch several rows in the outputted image, for example, if the chunk is 400
	         * pixels long. We will have to wrap it over 14 rows in the image.
	         *  If the chunk is transparent. Set that many pixels to be transparent.
	         *  If the chunk is pixel data, read from the cursor that many pixels. One pixel is 3 bytes in
	         * in RGB aligned data (eg. char R, char B, char G) so if the unsigned short says 20, we
	         * read 20*3 = 60 bytes.
	         *  Once we read one chunk, we switch to the other type of chunk (if we've just read a transparent
	         * chunk, we read a pixel chunk and vice versa). And then start over again.
	         *  All sprites start with a transparent chunk.
	         */

	        int bytes = 0;
	        int x = 0;
	        int y = 0;
	        int chunk_size;

	        while (bytes < Dump.Length && y < 32)
            {
		        chunk_size = Dump[bytes] | Dump[bytes+1] << 8;
		        //printf("pos:%d\n", bytes);
		        //printf("Reading transparent chunk size %d\n", int(chunk_size));
		        bytes += 2;

		        for(int i = 0; i < chunk_size; ++i)
                {
			        //printf("128*%d+%d*4+3\t= %d\n", y, x, 128*y+x*4+3);
			        rgba32x32[128*y+x*4+3] = 0x00; // Transparent pixel
			        x++;
			        if(x >= 32) {
				        x = 0;
				        y++;
				        if(y >= 32)
					        break;
			        }
		        }

		        if(bytes >= Dump.Length || y >= 32)
			        break; // We're done
		        // Now comes a pixel chunk, read it!
		        chunk_size = Dump[bytes] | Dump[bytes+1] << 8;
		        bytes += 2;
		        //printf("Reading pixel chunk size %d\n", int(chunk_size));
		        for(int i = 0; i < chunk_size; ++i)
                {
			        rgba32x32[128*y+x*4+0] = Dump[bytes+0]; // Red
			        rgba32x32[128*y+x*4+1] = Dump[bytes+1]; // Green
			        rgba32x32[128*y+x*4+2] = Dump[bytes+2]; // Blue
			        rgba32x32[128*y+x*4+3] = 0xFF; //Opaque pixel

			        bytes += 3;

			        x++;
			        if(x >= 32) {
				        x = 0;
				        y++;
				        if(y >= 32)
					        break;
			        }
		        }
	        }

	        // Fill up any trailing pixels
	        while(y<32 && x<32) {
		        rgba32x32[128*y+x*4+3] = 0x00; // Transparent pixel
		        x++;
		        if(x >= 32) {
			        x = 0;
			        y++;
		        }
	        }

	        return rgba32x32;
        }
    }
}
