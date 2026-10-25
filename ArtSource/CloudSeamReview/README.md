The old curtain backing used 325 separate narrow rectangles. Adjacent rectangles
could end at very different heights underneath the billows, exposing straight white
vertical pins through the cloud imagery during entry and exit.

The backing now forms one connected triangle strip, sharing both edge vertices
between columns. Each join uses the more conservative of its neighboring heights
so it stays inside the covered silhouette. Backing vertex count drops from 1300
to 652 per bank; sprites, artwork, travel speed and transition timing are unchanged.

Before.png and After.png show the same incoming pose at 1366×768. BeforeDetail.png
and AfterDetail.png are identical nearest-neighbor crops, enlarged four times.
For example, the thin bright pin near screen x=571, y=540–550 is removed.

ReviewCloudSeams.Run renders only the production UI, without Play mode. The isolated
review captured seven moving curtain poses at 1280×720, 640×480 and 1366×768.
Pixel inspection also checked for background holes in full cover and for clouds
leaking into the side margins. See Verification.txt for the recorded results.
