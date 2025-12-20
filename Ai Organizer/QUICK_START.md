# Quick Start Guide - AI Organizer

## Getting Started

### First Time Setup

1. **Launch the Application**
   - Opens to the "Setup" tab
   - Interactive wizard guides you through configuration

2. **Setup Tab - Organization Configuration (4 steps)**

   **Step 1: Organization Strategy**
   - Answer: "How would you like to organize your files?"
   - Examples: "by type", "by date", "by project", "by extension"
   - The app learns your preference and creates a system prompt

   **Step 2: Automation Level**
   - Slider from 0 to 1
   - 0 = Fully automatic (no questions asked)
   - 0.5 = Medium (ask about groups of files)
   - 1 = Manual (ask about every file)

   **Step 3: Constraints & Rules**
   - Specify rules to enforce
   - Examples: "max 2GB per folder", "preserve dates", "no system files"
   - Separated by commas or semicolons

   **Step 4: File Type Exclusions**
   - File types to skip
   - Examples: "exe,dll,sys,tmp"
   - Or type "none" to skip

3. **Review Advanced Options**
   - Toggle metadata updates
   - Set folder depth limit
   - Configure live scanning updates
   - Click "Submit Answer" when done

---

## Main Features

### Models Tab

**Browse & Download Models**
1. Click "Refresh" to load all available models
2. Select a model from the list
3. View details on the right (name, provider, size, performance)
4. Click "Download" to download the model
5. Watch progress bar for download status

**Model Information**
- **Provider**: Source of the model (Ollama, OpenAI, Gemini, Anthropic, HuggingFace)
- **Vision Support**: Whether the model can analyze images
- **Performance Score**: Quality rating based on your previous uses
- **Downloaded**: Green checkmark if downloaded locally

**Performance Rankings**
- Shows all models ranked by quality score
- Higher score = better performance on your tasks
- See average response time and total cost
- Use this to choose the best model for your needs

**Actions**
- **Download**: Get model for offline use (Ollama only)
- **Update**: Update to latest version
- **Delete**: Remove downloaded model and clear metrics

### Scan Tab

**File Selection**
1. Click "Add Folders" to select directories
2. Click "Add Files" to select individual files
3. Mix and match folders and files
4. "Clear Roots" to start over

**Scan Configuration**
- **Include Hidden**: Scan hidden files (default: off)
- **Max Depth**: How many folders deep to scan (default: 12)
- **Include Glob**: Pattern for files to include (default: `**/*`)
- **Exclude Glob**: Pattern for files to skip (default: `**/bin/**;**/obj/**`)

**Scanning**
1. Configure options above
2. Click "Scan" button
3. Watch real-time updates:
   - Directories visited
   - Files scanned
   - Files matched
4. Results appear in the table below

**Selection Management**
- **Select All**: Check all matched files
- **Select None**: Uncheck all files
- Remove individual roots with the X button

---

## Plan Preview Tab

**View Organization Plan**
1. After scanning files
2. Click "Generate Plan" with selected model
3. See proposed organization structure
4. Files grouped by destination folder
5. See confidence scores for each move

**Review & Execute**
- Check proposed actions
- Review confidence scores
- Click "Execute Plan" to move/copy files
- Monitor progress in real-time

---

## Settings Tab

**Configure Providers**

**Ollama Settings**
- Endpoint: Default `http://localhost:11434`
- Model: Default `llama3.2`
- Docker options: Enable Docker mode if running Ollama in container

**OpenAI Settings**
- Add your API key securely
- Select default model (e.g., `gpt-4o-mini`)
- Key is encrypted and never stored in plain text

**Google Gemini Settings**
- Add your API key from Google Cloud
- Default model: `gemini-pro`
- Supports latest Gemini models

**Anthropic Settings**
- Add your API key from Anthropic Console
- Default model: `claude-3-sonnet`
- Supports all Claude 3 variants

**Preferences**
- **Preferred Provider**: Choose which model to use by default
- **Default Prompt**: Custom system instruction for file organization
- **Performance Tracking**: Enable/disable metrics collection

---

## Workflow Example

### Scenario: Organize My Downloads Folder

1. **Setup Tab**
   - Answer: "Group files by type, then organize by date"
   - Handiness: 0.5 (ask about groups)
   - Constraints: "max 1GB per folder, keep file dates"
   - Exclude: "exe,msi,zip"
   - Submit

2. **Models Tab**
   - Review available models
   - Select "llama3.2" (high quality, fast)
   - Click "Download" if not already downloaded

3. **Scan Tab**
   - Add Folder: `C:\Users\You\Downloads`
   - Max Depth: 3
   - Click "Scan"
   - Wait for complete file list
   - Select All files (or choose specific ones)

4. **Plan Preview Tab**
   - Click "Generate Plan"
   - App analyzes files with selected model
   - See proposed organization:
     - `Documents/` (PDFs, DOCX, TXT)
     - `Images/` (JPG, PNG, GIF)
     - `Videos/` (MP4, MKV)
     - `Archives/` (RAR, 7Z, TAR)
   - Files grouped by month within each type
   - Confidence scores shown for each file

5. **Execute**
   - Review proposed moves
   - Click "Execute Plan"
   - Files organized according to plan
   - Model metrics recorded

6. **Monitor Performance**
   - Return to Models tab
   - Check updated performance score
   - See response time and cost
   - Use data to optimize future choices

---

## Tips & Tricks

### Performance Optimization
- **For Speed**: Use smaller models (llama2, claude-haiku)
- **For Quality**: Use larger models (gpt-4, claude-opus)
- **For Cost**: Use API-based models, monitor metrics

### Smart Constraints
- Set reasonable folder size limits (2GB-5GB typical)
- Always preserve original file dates if possible
- Test on a small folder first before full organization

### Configuration Profiles
- Document your setup choices in a text file
- Run multiple setups for different scenarios:
  - "Creative files" vs "Work documents"
  - "Quick cleanup" vs "Deep organization"

### Using Live Updates
- Large scans benefit from enabled live updates
- Watch progress in real-time
- Cancel anytime with "Cancel Scan" button

### API Key Security
- Keys never appear in settings file
- Stored encrypted via Windows Data Protection
- Each user account has separate secrets storage
- Export settings freely without exposing keys

---

## Common Issues

### Issue: Models Not Loading
**Solution**:
- Check internet connection
- Verify API keys in Settings
- Ensure provider is enabled
- Try clicking "Refresh" again

### Issue: Scan Taking Too Long
**Solution**:
- Reduce max depth
- Use more specific include glob
- Exclude more patterns (binaries, cache, etc.)
- Consider scanning smaller folders

### Issue: Plan Generation Failed
**Solution**:
- Check model is online (Ollama) or API working
- Verify API key not expired
- Try with different model
- Check system prompt in Settings

### Issue: Files Not Moving
**Solution**:
- Verify source files are readable
- Check destination has write permissions
- Ensure no files are locked/in use
- Try "Copy" instead of "Move"

---

## Advanced Usage

### Custom Prompts
In Settings, modify "Default Prompt" to guide organization:

Example:
```
Group by file type, then organize chronologically.
Prioritize readability and ease of access.
Create folders for documents, media, archives, and projects.
Keep related files together.
```

### Glob Patterns
Advanced folder structure selection:

```
Include: src/**/*.cs;docs/**/*.md    (only code and docs)
Exclude: bin/**;obj/**;.git/**      (skip build and version control)
```

### Natural Language Processing
Try various phrasings for constraints:

- "Keep files under 2 gigs per folder"
- "Maximum folder size is 2GB"
- "Don't exceed 2GB per directory"

All recognized and parsed the same way!

---

## Support

- For issues, check the IMPLEMENTATION_GUIDE.md for technical details
- Review error messages carefully - they guide troubleshooting
- Check settings to ensure all APIs are configured
- Try with smaller datasets first to test configuration

---

**Enjoy organizing with AI!** 🚀

