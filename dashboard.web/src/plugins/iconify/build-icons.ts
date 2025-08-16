/**
 * Advanced Iconify Bundle Script - ES Modules
 */
import { promises as fs } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

import { cleanupSVG, importDirectory, isEmptyColor, parseColors, runSVGO } from '@iconify/tools';
import type { IconifyJSON } from '@iconify/types';
import { getIcons, getIconsCSS, stringToIcon } from '@iconify/utils';

import riIcons from '@iconify-json/ri/icons.json';
import bxlIcons from '@iconify-json/bxl/icons.json';

/**
 * ES Modules replacement for __dirname
 */
const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

/**
 * Script configuration
 */
interface BundleScriptCustomSVGConfig {
  dir: string;
  monotone: boolean;
  prefix: string;
}

interface BundleScriptCustomJSONConfig {
  filename: string;
  icons?: string[];
}

interface BundleScriptConfig {
  svg?: BundleScriptCustomSVGConfig[];
  icons?: string[];
  json?: (string | BundleScriptCustomJSONConfig)[];
}

/**
 * Sources
 */
const sources: BundleScriptConfig = {
  svg: [],
  icons: [],
  json: [
    { filename: 'ri-icons.json', icons: Object.keys(riIcons.icons) },
    { filename: 'bxl-icons.json', icons: ['facebook', 'twitter', 'github', 'google', 'linkedin'] },
  ],
};

/**
 * Output file
 */
const target = join(__dirname, 'icons.css');

(async function () {
  // Create output directory if missing
  await fs.mkdir(dirname(target), { recursive: true });

  const allIcons: IconifyJSON[] = [];

  /**
   * Convert sources.icons to sources.json (if needed)
   */
  if (sources.icons) {
    const sourcesJSON = sources.json ?? (sources.json = []);
    const organizedList = organizeIconsList(sources.icons);

    for (const prefix in organizedList) {
      // En ESM no se puede usar require.resolve, aquí se omite
      sourcesJSON.push({ filename: `${prefix}.json`, icons: organizedList[prefix] });
    }
  }

  /**
   * Process JSON sources
   */
  if (sources.json) {
    for (const item of sources.json) {
      let content: IconifyJSON;

      if (typeof item === 'string') {
        content = JSON.parse(await fs.readFile(item, 'utf8'));
      } else {
        switch (item.filename) {
          case 'ri-icons.json':
            content = riIcons;
            break;
          case 'bxl-icons.json':
            content = bxlIcons;
            break;
          default:
            content = JSON.parse(await fs.readFile(item.filename, 'utf8'));
        }

        if (item.icons?.length) {
          const filtered = getIcons(content, item.icons);
          if (!filtered) throw new Error(`Cannot find required icons in ${item.filename}`);
          allIcons.push(filtered);
          continue;
        }
      }

      allIcons.push(content);
    }
  }

  /**
   * Process SVG sources
   */
  if (sources.svg) {
    for (const source of sources.svg) {
      const iconSet = await importDirectory(source.dir, { prefix: source.prefix });

      const iconNames: string[] = [];
      iconSet.forEach((name, type) => {
        if (type === 'icon') {
          iconNames.push(name);
        }
      });

      for (const name of iconNames) {
        const svg = iconSet.toSVG(name);
        if (!svg) {
          iconSet.remove(name);
          continue;
        }

        try {
          await cleanupSVG(svg);

          if (source.monotone) {
            await parseColors(svg, {
              defaultColor: 'currentColor',
              callback: (attr, colorStr, color) =>
                !color || isEmptyColor(color) ? colorStr : 'currentColor',
            });
          }

          await runSVGO(svg);
        } catch (err) {
          console.error(`Error parsing ${name} from ${source.dir}:`, err);
          iconSet.remove(name);
          continue;
        }

        iconSet.fromSVG(name, svg);
      }

      allIcons.push(iconSet.export());
    }
  }

  /**
   * Generate CSS
   */
  const cssContent = allIcons
    .map(iconSet =>
      getIconsCSS(iconSet, Object.keys(iconSet.icons), { iconSelector: '.{prefix}-{name}' })
    )
    .join('\n');

  await fs.writeFile(target, cssContent, 'utf8');
  console.log(`Saved CSS to ${target}!`);
})().catch(console.error);

/**
 * Helper: organize icons by prefix
 */
function organizeIconsList(icons: string[]): Record<string, string[]> {
  const sorted: Record<string, string[]> = Object.create(null);

  for (const icon of icons) {
    const item = stringToIcon(icon);
    if (!item) continue;

    const prefixList = sorted[item.prefix] ?? (sorted[item.prefix] = []);
    if (!prefixList.includes(item.name)) prefixList.push(item.name);
  }

  return sorted;
}
