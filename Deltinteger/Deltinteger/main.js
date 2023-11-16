import { dotnet } from './_framework/dotnet.js'

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

const { setModuleImports, getAssemblyExports, getConfig } = await dotnet.create();

setModuleImports("main.js", {
    window: {
        location: {
            href: () => window.location.href
        }
    },
    console: {
        log: (text) => console.log(text)
    },
    ostwWeb: {
        getWorkshopElements: async () => await window.ostwWeb.getWorkshopElements(),
        setDiagnostics: (publish) => window.ostwWeb.setDiagnostics(publish)
    }
});

const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);
window.ostw = exports.OstwJavascript;
window.onOstwReady();

// Runs Program.Main
// await dotnet.run();