using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.IO;

/// <summary>
/// Loads, saves and interprets keyActions dictionary
/// allowing customised configeration of keyboard actions
/// in wpf enviroment
/// <seealso cref=">keySelectWindow"/>
public class KeyActionClass
{
    public Dictionary<String, String> keyActions;
    public String[] keyList;
    public List<String> actionList;
    public String keyConfigFid;
    public int errorCount = 0;

    /// <summary>
    /// prepare dictionary of configured keyboard actions
    /// <param name="fid"> full path of keyboard action configuration file
    /// <param name="al"> string array of the possible keyboard actions
    public KeyActionClass(String fid, String[] al = null)
    {
        if (al == null)
            actionList = new List<String>();
        else
            actionList = al.ToList<String>();
        keyConfigFid = fid;
        loadKeyList();
        loadActionKeys(keyConfigFid);
    }

    /// <summary>
    /// <returns>Action desciptor for the given input key or None if not assigned
    public String getAction(Key k)
    {
        String ks = k.ToString();
        if (keyActions.Keys.Contains(ks))
            return keyActions[ks];
        else
            return "None";
    }

    /// <summary>
    /// Load keyList with all possible key descriptors
    public void loadKeyList()
    {
        List<String> kl = new List<String>();
        for (int ki = 0; ki < 256; ki++)  // typically less than 173
        {
            try
            {
                Key k = (Key)ki;
                String ks = k.ToString();
                try
                {
                    int.Parse(ks);
                }
                catch  // should catch for valid key
                {
                    kl.Add(ks);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in loadKeyList:" + e.ToString());
            }
        }
        keyList = kl.ToArray();
    }

    /// <summary>
    /// Diagnostic tool
    public void printKeys()
    {
        foreach (String k in keyList)
            Console.WriteLine(k);
    }

    /// <summary>
    /// Save key board action dictionary to file
    /// <param name="fid"> full path of keyboard action configuration file
    public void saveActionKeys(String fid)
    {
        try
        {
            // Write each directory name to a file. 
            using (StreamWriter sw = new StreamWriter(fid, false))  //do not append
            {
                foreach (String k in keyActions.Keys)
                {
                    sw.WriteLine(keyActions[k] + " = " + k);
                }
            }
        }
        catch (Exception ec)
        {
            System.Windows.MessageBox.Show("ERROR Saving pods heard" + ec.ToString());
        }
    }

    /// <summary>
    /// prepare dictionary of configured keyboard actions
    /// <param name="fid"> full path of keyboard action configuration file
    public void loadActionKeys(String fid)
    {
        keyActions = new Dictionary<String, String>();
        try
        {
            // Read and show each line from the file. 
            string line = "";
            using (StreamReader sr = new StreamReader(fid))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    String[] ka = line.Split("=".ToArray<char>());
                    if (ka.Length < 2)
                    {
                        Console.WriteLine("Needs action = key in line:'" + line + "' of file:" + fid);
                        errorCount++;
                        continue;
                    }
                    String act = ka[0].Trim();
                    if (!actionList.Contains(act))
                        actionList.Add(act);
                    String k = ka[1].Trim();
                    if (!keyList.Contains(k))
                    {
                        Console.WriteLine("Key not recognised in line:'" + line + "' of file:" + fid);
                        errorCount++;
                        continue;
                    }
                    keyActions[k] = act;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception in loadActionKeys:" + e.ToString());
            //System.Windows.MessageBox.Show("ERROR loading keyAction config:" + e.ToString());
        }
    }

    /// <summary>
    /// <param name="act">Action desciptor</param>
    /// <returns>Key descriptor for this action</returns>
    public String getKey(String act)
    {
        if (keyActions.Values.Contains(act))
        {
            int ki = keyActions.Values.ToList().IndexOf(act);
            String ak = keyActions.Keys.ToList()[ki];
            return ak;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Sets an item in the keyActions dictionary
    /// <param name="act">Action desciptor</param>
    /// <param name="act">Key descriptor</param>
    public void setKeyAction(String act, String key)
    {
        if (key != "None")
            keyActions[key] = act;
        else
        {
            foreach (String k in keyActions.Keys)
            {
                if (keyActions[k] == act)
                {
                    keyActions.Remove(k);
                    break;
                }
            }
        }
    }

}
