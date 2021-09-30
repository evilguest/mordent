#### String details
Strings are internally represented as an Utf8 strings, packed into a rope-like B+ tree structure:
1. The StringLeaf nodes are holding the string fragments encoded in UTF8, and take up to the whole page 
2. Whenever the Utf8 representation of a given string does no longer fit the single page, we're splitting it into the series of page-fitting fragments. 
3. The leaf pages form a double-linked list, pointing to each other. So, traversing through the string doesn't require reading the branch pages.
4. The branch pages do hold a map from the _character_ position to the child node.  
5. The short enough strings should be completely stored in-row.
6. The long strings can still hold their prefix in-row for efficiency.

Therefore, the string layout is as follows:

##### Short string (ASCII)
<table>
 <tr>
  <th>Byte offset</th><td colspan=8>0</td><td colspan=8>1</td><td>2</td><td>...</td>
 </tr>
 <tr>
  <th>Bit offset</th><td>0</td><td>1</td><td>2</td><td>3</td><td>4</td><td>5</td><td>6</td><td>7</td><td>8</td><td>9</td><td>10</td><td>11</td><td>12</td><td>13</td><td>14</td><td>15</td><td/><td/>
 </tr>
 <tr>
  <th>Data layout</th><td align=center colspan=14>String <b>length</b>: up to (16384 - overhead)</td><th>0</th><th>0</th><td colspan=2><b>length</b> bytes </td>
 </tr>
</table>

##### Short string (Utf8)
<table>
 <tr>
  <th>Byte offset</th><td colspan=8>0</td><td colspan=8>1</td><td>2</td><td>3</td><td>...</td>
 </tr>
 <tr>
  <th>Bit offset</th><td>0</td><td>1</td><td>2</td><td>3</td><td>4</td><td>5</td><td>6</td><td>7</td><td>8</td><td>9</td><td>10</td><td>11</td><td>12</td><td>13</td><td>14</td><td>15</td><td/><td/><td/>
 </tr>
 <tr>
  <th>Data layout</th><td align=center colspan=14>String <b>length</b>: up to (16384 - overhead)</td><th>0</th><th>1</th> <td colspan=2>String <b>bytes</b> count</td><td colspan=2><b>bytes</b> bytes representing the string in utf8</td>
 </tr>
</table>

##### Long string (ASCII prefix)
<table>
 <tr>
  <th>Byte offset</th><td colspan=8>0</td><td colspan=8>1</td><td>2</td><td>3</td><td>4</td><td>5</td><td>6</td><td>7</td><td>8</td><td>9</td><td>10</td><td>11</td><td>12</td><td>...</td>
 </tr>
 <tr>
  <th>Bit offset</th><td>0</td><td>1</td><td>2</td><td>3</td><td>4</td><td>5</td><td>6</td><td>7</td><td>8</td><td>9</td><td>10</td><td>11</td><td>12</td><td>13</td><td>14</td><td>15</td><td/><td/><td/><td/><td/><td/><td/><td/><td/><td/><td/><td/>
 </tr>
 <tr>
  <th>Data layout</th><td align=center colspan=14>String <b>prefix length</b>: up to (16384 - overhead)</td><th>1</th><th>0</th> <td colspan=4>String <b>complete length</b>: up to 2GB</td><td colspan=4>string overflow PageNo</td><td colspan=2>string overflow FileNo</td><td colspan=2><b>prefix length</b> bytes representing the string prefix in ASCII</td>
 </tr>
</table>

##### Long string (Utf8 prefix)
<table>
 <tr>
  <th>Byte offset</th><td colspan=8>0</td><td colspan=8>1</td><td>2</td><td>3</td><td>4</td><td>5</td><td>6</td><td>7</td><td>8</td><td>9</td><td>10</td><td>11</td><td>12</td><td>13</td><td>14</td><td>...</td>
 </tr>
 <tr>
  <th>Bit offset</th><td>0</td><td>1</td><td>2</td><td>3</td><td>4</td><td>5</td><td>6</td><td>7</td><td>8</td><td>9</td><td>10</td><td>11</td><td>12</td><td>13</td><td>14</td><td>15</td><td/><td/><td/><td/><td/><td/><td/><td/><td/><td/><td/><td/><td/><td/>
 </tr>
 <tr>
  <th>Data layout</th><td align=center colspan=14>String <b>prefix length</b>: up to (16384 - overhead)</td><th>1</th><th>1</th> <td colspan=2>String <b>prefix bytes</b> count</td><td colspan=4>String <b>complete length</b>: up to 2GB</td><td colspan=4>string overflow PageNo</td><td colspan=2>string overflow FileNo</td><td colspan=2><b>prefix bytes</b> bytes representing the string prefix in utf8</td>
 </tr>
</table>

##### String overflow page header (ASCII)
<table>
 <tr>
  <th>Byte offset</th><td colspan=8>0</td><td colspan=8>1</td><td>2</td><td>3</td><td>4</td><td>5</td><td>6</td><td>7</td><td>8</td><td>9</td><td>10</td><td>11</td><td>12</td><td>13</td><td>14</td><td>...</td>
 </tr>
 <tr>
  <th>Bit offset</th><td>0</td><td>1</td><td>2</td><td>3</td><td>4</td><td>5</td><td>6</td><td>7</td><td>8</td><td>9</td><td>10</td><td>11</td><td>12</td><td>13</td><td>14</td><td>15</td><td/><td/><td/><td/><td/><td/><td/><td/><td/><td/><td/><td/><td/><td/>
 </tr>
 <tr>
  <th>Data layout</th><td align=center colspan=14>String <b>fragment length</b>: up to (16384 - overhead)</td><th>0</th><th>0</th><td colspan=2>String <b>prefix bytes</b> count</td><td colspan=4>String <b>complete length</b>: up to 2GB</td><td colspan=4>string overflow PageNo</td><td colspan=2>string overflow FileNo</td><td colspan=2><b>prefix bytes</b> bytes representing the string prefix in utf8</td>
 </tr>
</table>

*Note*: this worth investigating in a separate project.
