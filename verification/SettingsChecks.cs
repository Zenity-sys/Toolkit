using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using KavrosClueHelper;

public sealed class SettingsChecks : MarshalByRefObject
{
    [STAThread]
    public static int Main(string[] args)
    {
        var setup=new AppDomainSetup{ApplicationBase=System.IO.Path.GetFullPath(args[0])};
        AppDomain domain=AppDomain.CreateDomain("Packaged settings",null,setup);
        try
        {
            var worker=(SettingsChecks)domain.CreateInstanceFromAndUnwrap(
                Assembly.GetExecutingAssembly().Location,typeof(SettingsChecks).FullName);
            worker.Check();
            return 0;
        }
        catch(Exception e){Console.Error.WriteLine(e);return 1;}
        finally{AppDomain.Unload(domain);}
    }
    public void Check()
    {
        Application.EnableVisualStyles();
        using(var form=new PuzzleSuiteForm(false))
        {
            var footer=form.Controls.Find("BuildCredit",true).OfType<Label>().Single();
            var crest=form.Controls.Find("KavrosCrest",true).OfType<PictureBox>().Single();
            if(footer.Text!="Made by Zenity  ·  v0.9.13"||form.Text!="Kavros Clue Helper · v0.9.13"||crest.Image==null||form.Icon==null)throw new Exception("Missing branding or incorrect version: "+footer.Text+" / "+form.Text);
            if(form.Controls.Find("ModernInterface",true).Length!=1||form.Controls.Find("LegacyInterface",true).Length!=1)throw new Exception("Interface toggles are missing.");
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var option=(CheckBox)typeof(PuzzleSuiteForm).GetField("reverse",flags).GetValue(form);
            var summary=(Label)typeof(PuzzleSuiteForm).GetField("summary",flags).GetValue(form);
            float[] factors=new float[]{1.25f,1.2f,4f/3f};
            string[] names=new string[]{"100%","125%","150%","200%"};
            for(int i=0;i<names.Length;i++){
                if(option.Parent!=summary.Parent||option.Bottom>summary.Top||option.GetPreferredSize(Size.Empty).Height>option.Height)
                    throw new Exception("Settings text clipped at "+names[i]);
                CheckClientLayout(form,names[i]);
                Console.WriteLine("Settings checkbox ends at "+option.Bottom+", next label starts at "+summary.Top);
                if(i<factors.Length)form.Scale(new SizeF(factors[i],factors[i]));
            }
            Console.WriteLine("PASS Settings branding and layout at 100%, 125%, 150% and 200%.");
        }
    }
    private static void CheckClientLayout(Form form,string scale)
    {
        Control body=form.Controls.Find("SettingsBody",true)[0];
        Button whatsNew=null;
        foreach(Control c in body.Controls)if(c is Button&&c.Text=="What's new")whatsNew=(Button)c;
        Label interfaceLabel=(Label)form.Controls.Find("InterfaceLabel",true)[0];
        Label resolutionLabel=(Label)form.Controls.Find("ResolutionLabel",true)[0];
        Label scalingLabel=(Label)form.Controls.Find("ScalingLabel",true)[0];
        Control legacy=form.Controls.Find("LegacyInterface",true)[0];
        Control resolution=form.Controls.Find("ResolutionChoice",true)[0];
        Control scaling=form.Controls.Find("ScalingChoice",true)[0];
        if(whatsNew==null)throw new Exception("What's new button is missing.");
        if(interfaceLabel.Top<=whatsNew.Bottom)throw new Exception(scale+": What's new overlaps the Client section ("+whatsNew.Bottom+" / "+interfaceLabel.Top+").");
        if(resolution.Top<=legacy.Bottom||scaling.Top<=legacy.Bottom)throw new Exception(scale+": Resolution or Scaling overlaps the Client toggles.");
        if(resolutionLabel.BackColor.ToArgb()!=resolutionLabel.Parent.BackColor.ToArgb()||scalingLabel.BackColor.ToArgb()!=scalingLabel.Parent.BackColor.ToArgb())
            throw new Exception(scale+": Resolution or Scaling label has its own background.");
        if(interfaceLabel.BackColor.ToArgb()==interfaceLabel.Parent.BackColor.ToArgb())
            throw new Exception(scale+": Interface label does not sit on the Client section.");
        if(footerClipped(form))throw new Exception(scale+": the footer is clipped.");
        int lowest=0;
        foreach(Control c in body.Controls)if(c.Bottom>lowest)lowest=c.Bottom;
        int scrollHeight=((ScrollableControl)body).AutoScrollMinSize.Height;
        if(scrollHeight<lowest)throw new Exception(scale+": Settings body is shorter than its contents ("+scrollHeight+" < "+lowest+").");
        Console.WriteLine(scale+": What's new ends "+whatsNew.Bottom+", Interface at "+interfaceLabel.Top+", Resolution at "+resolution.Top+", content "+lowest+".");
    }
    private static bool footerClipped(Form form)
    {
        Label footer=(Label)form.Controls.Find("BuildCredit",true)[0];
        return footer.Height<footer.GetPreferredSize(Size.Empty).Height||footer.Bottom>form.ClientSize.Height+1;
    }
}
