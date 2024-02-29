import { dotnet } from './_framework/dotnet.js'

// const is_browser = typeof window != "undefined";
// if (!is_browser) throw new Error(`Expected to be running in a browser`);

console.log('⭐ set up ostw dotnet ⭐');

const { setModuleImports, getAssemblyExports, getConfig } = await dotnet.create();

const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);

export const ostw = exports.OstwJavascript;

export function setImports(
    getWorkshopElements,
    getLobbySettings,
    getMaps,
    setDiagnostics,
    setCompiledWorkshopCode,
    onNotification
) {
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
            getWorkshopElements: getWorkshopElements,
            getLobbySettings: getLobbySettings,
            getMaps: getMaps,
            setDiagnostics: setDiagnostics,
            setCompiledWorkshopCode: setCompiledWorkshopCode,
            onNotification: onNotification
        }
    });
}

console.log('⭐ ostw dotnet ready ⭐');

// Runs Program.Main
// await dotnet.run();