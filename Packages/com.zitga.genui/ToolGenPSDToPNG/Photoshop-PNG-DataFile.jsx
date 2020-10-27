// This script exports photoshop layers as individual PNGs. It also
// writes a JSON file that can be imported into Spine where the images
// will be displayed in the same positions and draw order.

// Setting defaults.
var writePngs = true;
var writeTemplate = false;
var writeJson = true;
var ignoreHiddenLayers = true;
var pngScale = 1;
var groupsAsSkins = false;
var useRulerOrigin = false;
var imagesDir = "./images/";
var projectDir = "";
var padding = 0;

// IDs for saving settings.
const settingsID = stringIDToTypeID("settings");
const writePngsID = stringIDToTypeID("writePngs");
const writeTemplateID = stringIDToTypeID("writeTemplate");
const writeJsonID = stringIDToTypeID("writeJson");
const ignoreHiddenLayersID = stringIDToTypeID("ignoreHiddenLayers");
const groupsAsSkinsID = stringIDToTypeID("groupsAsSkins");
const useRulerOriginID = stringIDToTypeID("useRulerOrigin");
const pngScaleID = stringIDToTypeID("pngScale");
const imagesDirID = stringIDToTypeID("imagesDir");
const projectDirID = stringIDToTypeID("projectDir");
const paddingID = stringIDToTypeID("padding");

var listError = [];
var originalDoc;
try {
	originalDoc = app.activeDocument;
} catch (ignored) { }
var settings, progress;
loadSettings();
showDialog();

function run() {
	// Output dirs.
	var absProjectDir = absolutePath(projectDir);
	new Folder(absProjectDir).create();
	var absImagesDir = absolutePath(imagesDir);
	var imagesFolder = new Folder(absImagesDir);
	imagesFolder.create();
	var relImagesDir = imagesFolder.getRelativeURI(absProjectDir);
	relImagesDir = relImagesDir == "." ? "" : (relImagesDir + "/");

	// Get ruler origin.
	var xOffSet = 0, yOffSet = 0;
	if (useRulerOrigin) {
		var ref = new ActionReference();
		ref.putEnumerated(charIDToTypeID("Dcmn"), charIDToTypeID("Ordn"), charIDToTypeID("Trgt"));
		var desc = executeActionGet(ref);
		xOffSet = desc.getInteger(stringIDToTypeID("rulerOriginH")) >> 16;
		yOffSet = desc.getInteger(stringIDToTypeID("rulerOriginV")) >> 16;
	}

	activeDocument.duplicate();

	// Output template image.
	if (writeTemplate) {
		if (pngScale != 1) {
			scaleImage();
			storeHistory();
		}

		var file = new File(absImagesDir + "template");
		if (file.exists) file.remove();

		activeDocument.saveAs(file, new PNGSaveOptions(), true, Extension.LOWERCASE);

		if (pngScale != 1) restoreHistory();
	}

	if (!writeJson && !writePngs) {
		activeDocument.close(SaveOptions.DONOTSAVECHANGES);
		return;
	}

	// Rasterize all layers.
	try {
		executeAction(stringIDToTypeID("rasterizeAll"), undefined, DialogModes.NO);
	} catch (ignored) { }

	// Collect and hide layers.
	var layers = [];
	collectLayers(activeDocument, layers);
	var layersCount = layers.length;

	// Check Name Convention
	if (listError.length > 0) {
		var message = "The layout name must contain only lowercase letters, numbers, and underscores.\n\n";
		for (var index = 0; index < listError.length; index++) {
			var element = listError[index];
			message += element;
			if (index < listError.length - 1)
				message += "\n";
		}

		alert(message);
		return;
	}

	storeHistory();

	// Store the slot names and layers for each skin.	
	var slots = {}, skins = { "default": [] };
	for (var i = layersCount - 1; i >= 0; i--) {
		var layer = layers[i];

		// Use groups as skin names.
		var potentialSkinName = trim(layer.parent.name);
		var layerGroupSkin = potentialSkinName.indexOf("-NOSKIN") == -1;
		var skinName = (groupsAsSkins && layer.parent.typename == "LayerSet" && layerGroupSkin) ? potentialSkinName : "default";
	
		var skinLayers = skins[skinName];
		if (!skinLayers) skins[skinName] = skinLayers = [];
		skinLayers[skinLayers.length] = layer;

		slots[layerName(layer)] = true;
	}
	
	var json = ''

	// Output skins.
	var skinsCount = countAssocArray(skins);
	var skinIndex = 0;
	for (var skinName in skins) {
		if (!skins.hasOwnProperty(skinName)) continue;

		var skinLayers = skins[skinName];
		var skinLayersCount = skinLayers.length;
		var skinLayerIndex = 0;
		for (var i = skinLayersCount - 1; i >= 0; i--) {
			var layer = skinLayers[i];
			var slotName = layerName(layer).split(' ').join('').toLowerCase();

			if (skinName == "default") {
				attachmentName = slotName;
			} else {
				attachmentName = skinName + "/" + slotName;
			}

			var x = activeDocument.width.as("px") * pngScale;
			var y = activeDocument.height.as("px") * pngScale;

			layer.visible = true;
			if (!layer.isBackgroundLayer) activeDocument.trim(TrimType.TRANSPARENT, false, true, true, false);
			x -= activeDocument.width.as("px") * pngScale;
			y -= activeDocument.height.as("px") * pngScale;
			if (!layer.isBackgroundLayer) activeDocument.trim(TrimType.TRANSPARENT, true, false, false, true);
			var width = activeDocument.width.as("px") * pngScale + padding * 2;
			var height = activeDocument.height.as("px") * pngScale + padding * 2;

			// Save image.
			if (writePngs) {
				if (pngScale != 1) scaleImage();
				if (padding > 0) activeDocument.resizeCanvas(width, height, AnchorPosition.MIDDLECENTER);

				if (skinName != "default") new Folder(absImagesDir + skinName).create();
				activeDocument.saveAs(new File(absImagesDir + attachmentName), new PNGSaveOptions(), true, Extension.LOWERCASE);
			}

			restoreHistory();
			layer.visible = false;

			x += Math.round(width) / 2;
			y += Math.round(height) / 2;

			// Make relative to the Photoshop document ruler origin.
			if (useRulerOrigin) {
				x -= xOffSet * pngScale;
				y -= activeDocument.height.as("px") * pngScale - yOffSet * pngScale; // Invert y.
			}

			var skinLink = layer.name + "[" + getIndexLayer(layer) + "]";
			var parentLayer = layer.parent;
			while (parentLayer != null) {
				skinLink = parentLayer.name + "[" + getIndexLayer(parentLayer) + "]" + "/" + skinLink;
				var childLayer = parentLayer;
				if (parentLayer.name == "root") {
					parentLayer = null;
				}
				else {
					parentLayer = parentLayer.parent;
				}
			}
			json += skinLink + ',' + x + ',' + y + ',' + width + ',' + height + '\n';

			skinLayerIndex++;
			if (parentLayer != null)
				alert(parentLayer.parent + "|" + parentLayer.name + "|" + layer.name);
		}

		skinIndex++;
	}

	activeDocument.close(SaveOptions.DONOTSAVECHANGES);

	// Output JSON file.
	if (writeJson) {
		var name = decodeURI(originalDoc.name);
		name = name.substring(0, name.indexOf("."));
		var file = new File(absImagesDir + name + ".csv");
		file.remove();
		file.open("w", "TEXT");
		file.lineFeed = "\n";
		file.write(json);
		file.close();
	}

	alert("Finished export layers.\n Join your time !!!");
}

function getIndexLayer(layer) {
	var parent = layer.parent;
	if (parent.layers) {
		for (var i = 0; i < parent.layers.length; i++) {
			if (parent.layers[i] == layer) {
				return parent.layers.length - i - 1;
			}
		}
	}
	return 0;
}
// Dialog and settings:

function showDialog() {
	if (!originalDoc) {
		alert("Please open a document before running the LayersToPNG script.");
		return;
	}
	if (!hasFilePath()) {
		alert("Please save the document before running the LayersToPNG script.");
		return;
	}

	var dialog = new Window("dialog", "Spine LayersToPNG");
	dialog.alignChildren = "fill";

	var checkboxGroup = dialog.add("group");
	var group = checkboxGroup.add("group");
	group.orientation = "column";
	group.alignChildren = "left";
	var writePngsCheckbox = group.add("checkbox", undefined, " Write layers as PNGs");
	writePngsCheckbox.value = writePngs;
	var writeTemplateCheckbox = group.add("checkbox", undefined, " Write a template PNG");
	writeTemplateCheckbox.value = writeTemplate;
	var writeJsonCheckbox = group.add("checkbox", undefined, " Write Export UI CSV");
	writeJsonCheckbox.value = writeJson;
	group = checkboxGroup.add("group");
	group.orientation = "column";
	group.alignChildren = "left";
	var ignoreHiddenLayersCheckbox = group.add("checkbox", undefined, " Ignore hidden layers");
	ignoreHiddenLayersCheckbox.value = ignoreHiddenLayers;
	var groupsAsSkinsCheckbox = group.add("checkbox", undefined, " Use groups as skins");
	groupsAsSkinsCheckbox.value = groupsAsSkins;
	var useRulerOriginCheckbox = group.add("checkbox", undefined, " Use ruler origin as 0,0");
	useRulerOriginCheckbox.value = useRulerOrigin;

	var slidersGroup = dialog.add("group");
	group = slidersGroup.add("group");
	group.orientation = "column";
	group.alignChildren = "right";
	group.add("statictext", undefined, "PNG scale:");
	group.add("statictext", undefined, "Padding:");
	group = slidersGroup.add("group");
	group.orientation = "column";
	var scaleText = group.add("edittext", undefined, pngScale * 100);
	scaleText.characters = 4;
	var paddingText = group.add("edittext", undefined, padding);
	paddingText.characters = 4;
	group = slidersGroup.add("group");
	group.orientation = "column";
	group.add("statictext", undefined, "%");
	group.add("statictext", undefined, "px");
	group = slidersGroup.add("group");
	group.alignment = ["fill", ""];
	group.orientation = "column";
	group.alignChildren = ["fill", ""];
	var scaleSlider = group.add("slider", undefined, pngScale * 100, 1, 100);
	var paddingSlider = group.add("slider", undefined, padding, 0, 4);
	scaleText.onChanging = function () { scaleSlider.value = scaleText.text; };
	scaleSlider.onChanging = function () { scaleText.text = Math.round(scaleSlider.value); };
	paddingText.onChanging = function () { paddingSlider.value = paddingText.text; };
	paddingSlider.onChanging = function () { paddingText.text = Math.round(paddingSlider.value); };

	var outputGroup = dialog.add("panel", undefined, "Output directories");
	outputGroup.alignChildren = "fill";
	outputGroup.margins = [10, 15, 10, 10];
	var textGroup = outputGroup.add("group");
	group = textGroup.add("group");
	group.orientation = "column";
	group.alignChildren = "right";
	group.add("statictext", undefined, "Images:");
	group.add("statictext", undefined, "JSON:");
	group = textGroup.add("group");
	group.orientation = "column";
	group.alignChildren = "fill";
	group.alignment = ["fill", ""];
	var imagesDirText = group.add("edittext", undefined, imagesDir);
	var projectDirText = group.add("edittext", undefined, projectDir);
	outputGroup.add("statictext", undefined, "Begin paths with \"./\" to be relative to the PSD file.").alignment = "center";

	var group = dialog.add("group");
	group.alignment = "center";
	var runButton = group.add("button", undefined, "OK");
	var cancelButton = group.add("button", undefined, "Cancel");
	cancelButton.onClick = function () {
		dialog.close(0);
		return;
	};

	function updateSettings() {
		writePngs = writePngsCheckbox.value;
		writeTemplate = writeTemplateCheckbox.value;
		writeJson = writeJsonCheckbox.value;
		ignoreHiddenLayers = ignoreHiddenLayersCheckbox.value;
		var scaleValue = parseFloat(scaleText.text);
		if (scaleValue > 0 && scaleValue <= 100) pngScale = scaleValue / 100;
		groupsAsSkins = groupsAsSkinsCheckbox.value;
		useRulerOrigin = useRulerOriginCheckbox.value;
		imagesDir = imagesDirText.text;
		projectDir = projectDirText.text;
		var paddingValue = parseInt(paddingText.text);
		if (paddingValue >= 0) padding = paddingValue;
	}

	dialog.onClose = function () {
		updateSettings();
		saveSettings();
	};

	runButton.onClick = function () {
		if (scaleText.text <= 0 || scaleText.text > 100) {
			alert("PNG scale must be between > 0 and <= 100.");
			return;
		}
		if (paddingText.text < 0) {
			alert("Padding must be >= 0.");
			return;
		}
		dialog.close(0);

		var rulerUnits = app.preferences.rulerUnits;
		app.preferences.rulerUnits = Units.PIXELS;
		try {
			run();
		} catch (e) {
			alert("An unexpected error has occurred.\n\nTo debug, run the LayersToPNG script using Adobe ExtendScript "
				+ "with \"Debug > Do not break on guarded exceptions\" unchecked.");
			debugger;
		} finally {
			if (activeDocument != originalDoc) activeDocument.close(SaveOptions.DONOTSAVECHANGES);
			app.preferences.rulerUnits = rulerUnits;
		}
	};

	dialog.center();
	dialog.show();
}

function loadSettings() {
	try {
		settings = app.getCustomOptions(settingsID);
	} catch (e) {
		saveSettings();
	}
	if (typeof settings == "undefined") saveSettings();
	settings = app.getCustomOptions(settingsID);
	if (settings.hasKey(writePngsID)) writePngs = settings.getBoolean(writePngsID);
	if (settings.hasKey(writeTemplateID)) writeTemplate = settings.getBoolean(writeTemplateID);
	if (settings.hasKey(writeJsonID)) writeJson = settings.getBoolean(writeJsonID);
	if (settings.hasKey(ignoreHiddenLayersID)) ignoreHiddenLayers = settings.getBoolean(ignoreHiddenLayersID);
	if (settings.hasKey(pngScaleID)) pngScale = settings.getDouble(pngScaleID);
	if (settings.hasKey(groupsAsSkinsID)) groupsAsSkins = settings.getBoolean(groupsAsSkinsID);
	if (settings.hasKey(useRulerOriginID)) useRulerOrigin = settings.getBoolean(useRulerOriginID);
	if (settings.hasKey(imagesDirID)) imagesDir = settings.getString(imagesDirID);
	if (settings.hasKey(projectDirID)) projectDir = settings.getString(projectDirID);
	if (settings.hasKey(paddingID)) padding = settings.getDouble(paddingID);
}

function saveSettings() {
	var settings = new ActionDescriptor();
	settings.putBoolean(writePngsID, writePngs);
	settings.putBoolean(writeTemplateID, writeTemplate);
	settings.putBoolean(writeJsonID, writeJson);
	settings.putBoolean(ignoreHiddenLayersID, ignoreHiddenLayers);
	settings.putDouble(pngScaleID, pngScale);
	settings.putBoolean(groupsAsSkinsID, groupsAsSkins);
	settings.putBoolean(useRulerOriginID, useRulerOrigin);
	settings.putString(imagesDirID, imagesDir);
	settings.putString(projectDirID, projectDir);
	settings.putDouble(paddingID, padding);
	app.putCustomOptions(settingsID, settings, true);
}

// Photoshop utility:

function scaleImage() {
	var imageSize = activeDocument.width.as("px");
	activeDocument.resizeImage(UnitValue(imageSize * pngScale, "px"), null, null, ResampleMethod.BICUBICSHARPER);
}

var historyIndex;
function storeHistory() {
	historyIndex = activeDocument.historyStates.length - 1;
}
function restoreHistory() {
	activeDocument.activeHistoryState = activeDocument.historyStates[historyIndex];
}

function collectLayers(layer, collect) {	
	for (var i = 0, n = layer.layers.length; i < n; i++) {
		var child = layer.layers[i];
		if (!checkNameConvention(child.name)) {
			listError.push(child.name)			
		}

		if (ignoreHiddenLayers && !child.visible) continue;
		if (child.bounds[2] == 0 && child.bounds[3] == 0) continue;
		if (child.layers && child.layers.length > 0)
			collectLayers(child, collect);
		else if (child.kind == LayerKind.NORMAL) {
			collect.push(child);
			child.visible = false;
		}
	}
}

function hasFilePath() {
	var ref = new ActionReference();
	ref.putEnumerated(charIDToTypeID("Dcmn"), charIDToTypeID("Ordn"), charIDToTypeID("Trgt"));
	return executeActionGet(ref).hasKey(stringIDToTypeID("fileReference"));
}

function absolutePath(path) {
	path = trim(path);
	if (path.length == 0)
		path = activeDocument.path.toString();
	else if (imagesDir.indexOf("./") == 0)
		path = activeDocument.path + path.substring(1);
	path = path.replace(/\\/g, "/");
	if (path.substring(path.length - 1) != "/") path += "/";
	return path;
}

// JavaScript utility:

function countAssocArray(obj) {
	var count = 0;
	for (var key in obj)
		if (obj.hasOwnProperty(key)) count++;
	return count;
}

function trim(value) {
	return value.replace(/^\s+|\s+$/g, "");
}

function endsWith(str, suffix) {
	return str.indexOf(suffix, str.length - suffix.length) !== -1;
}

function stripSuffix(str, suffix) {
	if (endsWith(str.toLowerCase(), suffix.toLowerCase())) str = str.substring(0, str.length - suffix.length);
	return str;
}

function layerName(layer) {	
	return stripSuffix(trim(layer.name), ".png").replace(/[:\/\\*\?\"\<\>\|]/g, "");
}

function checkNameConvention(nameLayout) {
	const regex = RegExp('^[a-z0-9_]*$');
	return regex.test(nameLayout)
}
