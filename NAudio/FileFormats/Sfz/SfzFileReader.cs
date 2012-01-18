using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace NAudio.Sfz
{
    enum SfzParseState
    {
        Initial,
        Region,
    }

    class Group
    {
    }

    class Region
    {
    }

    class SfzFileReader
    {
        public SfzFileReader(string fileName)
        {
            StringBuilder trimmed = new StringBuilder();
            using (StreamReader reader = new StreamReader(fileName))
            {
                //SfzParseState parseState = SfzParseState.Initial;
                string line;
                List<Region> regions = new List<Region>();
                Group currentGroup = null;
                Region currentRegion = null;
                StringBuilder currentOpcode = new StringBuilder();
                StringBuilder currentValue = new StringBuilder();
                int lastSpace;
                int lineNumber = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;
                    // 1. Strip comments
                    int commentIndex = line.IndexOf('/');
                    if(commentIndex != -1)
                        line = line.Substring(commentIndex);
                    
                    


                    for (int n = 0; n < line.Length; n++)
                    {   
                        
                        char c = line[n];
                        if(Char.IsWhiteSpace(c))
                        {
                            if (currentOpcode.Length == 0)
                            {
                                continue;
                            }
                            else if (currentValue.Length == 0)
                            {
                                throw new FormatException(String.Format("Invalid Whitespace Line {0}, Char {1}", lineNumber, n));
                            }
                            else
                            {
                                lastSpace = n;
                                currentValue.Append(c);
                            }
                        }
                        else if (c == '=')
                        {
                        }
                        else if (c == '<')
                        {
                            if (line.Substring(n).StartsWith("<region>"))
                            {
                                if (currentRegion != null)
                                    regions.Add(currentRegion);
                                currentRegion = new Region(); //currentGroup);
                                currentOpcode.Length = 0;
                                currentValue.Length = 0;
                                n += 7;
                            }
                            else if (line.Substring(n).StartsWith("<group>"))
                            {
                                if (currentRegion != null)
                                    regions.Add(currentRegion);
                                currentOpcode.Length = 0;
                                currentValue.Length = 0;
                                currentRegion = null;
                                currentGroup = new Group();
                                n += 6;
                            }
                            else
                            {
                                throw new FormatException(String.Format("Unrecognised section Line {0}, Char {1}", lineNumber, n));
                            }
                        }
                        else
                        {
                            //if(current.Length 
                        }

                        
                    }
                }
                
            }

        }
    }
}
