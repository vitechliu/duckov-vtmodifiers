/**
 * VT Modifier Groups 管理器
 * 用于查看和创建 VTModifierGroup JSON 文件
 */

// 全局变量
let currentJsonData = null;
let currentFileName = null;
let isEditingExisting = false;
let modifierCounter = 0;
let dataFieldCounter = 0;

const vtms = {
    PriceMultiplier: "价格倍率(必选)",
    
    DamageMultiplier: "伤害倍率",
    BulletSpeedMultiplier: "弹速倍率",
    ShootDistanceMultiplier: "射程/攻击距离倍率",
    ShootSpeedMultiplier: "射速/挥舞速度倍率",
    CritDamageMultiplier: "暴伤加成",
    ArmorPiercing: "穿甲",
    Penetrate: "穿透",
    SoundRange: "声音范围",
    ReloadTimeMultiplier: "换弹时间",
    ScatterMultiplier: "腰射精度",
    ScatterADSMultiplier: "瞄准精度",
    RecoilVMultiplier: "垂直后坐力",
    RecoilHMultiplier: "水平后坐力",
    TraceAbility: "子弹追踪",
    BurstCount: "连发数",
    ShootSpeedGainEachShoot: "每发射速叠加",
    ShootSpeedGainByShootMax: "射速叠加上限",
    DmgOverDistance: "超距离伤害系数",
    ControlMindType: "心控类型",
    ControlMindTime: "心控时间",
    
    
    WalkSoundRange: "行走声音范围",
    RunSoundRange: "奔跑声音范围",
    Armor: "护甲加成",
    ViewAngle: "视野范围",
    GasMask: "毒气防护",
    InventoryCapacity: "背包容量",
    MaxWeight: "负重",
    Moveability: "移动能力(移动速度系数)",
    RunAcc: "奔跑加速度",
    SenseRange: "感知距离",
    ColdProtection: "寒冷防护",
    StormProtection: "风暴防护",
    MaxHealth: "最大生命",
    MaxEnergy: "最大耐力",
    
    BleedChance: "流血概率",
    Weight: "重量修正",
    AmmoSave: "弹药节省率",

    ElementFire: "火元素加成",
    ElementSpace: "空间元素加成",
    ElementPoison: "毒元素加成",
    ElementElectricity: "电元素加成",
    ElementIce: "冰元素加成",
    ElementGhost: "灵元素加成",
    ElementPhysic: "物理抗性",

    StaminaCost: "耐力消耗",
    MaxStamina: "耐力上限",
    CritRate: "暴击率",
    Endurance: "不消耗耐久概率",
    LifeSteal: "吸血",
    DeathRate: "即死",
    DodgeRate: "闪避率",

    MaxMana: "最大魔量",
    MagicPower: "法术强度",
    MagicDistanceMultiplier: "法术距离",
    ManaCost: "魔量消耗",
    MagicCritRate: "法术暴击",
    CastTime: "咏唱时间",
}
// 预定义的data字段键（根据你的C# struct中的可能值）
// const predefinedDataKeys = [
//     "PriceMultiplier", "Moveability", "Damage", "Accuracy", "Recoil",
//     "Weight", "Ergonomics", "Durability", "Storage", "Protection",
//     "SoundReduction", "TurnSpeed", "MagazineCapacity", "FireRate",
//     "MalfunctionChance", "RicochetChance", "FragmentationChance"
// ];

// 当页面加载完成时初始化
document.addEventListener('DOMContentLoaded', function() {
    // 初始化代码高亮
    hljs.highlightAll();
    
    // 绑定事件监听器
    bindEvents();
    
    // 加载JSON文件列表
    loadJsonFileList();
    
    //为select注入options
    //此时获取不到select 
    
    
});

function fillSelect(selectElement) {
    selectElement.options.add(new Option("选择键...", ""));
    for (const key in vtms) {
        selectElement.options.add(new Option(vtms[key], key));
    }
    selectElement.options.add(new Option("自定义", "custom"));
}

// 绑定所有事件
function bindEvents() {
    // 导航按钮
    document.getElementById('showListBtn').addEventListener('click', showFileList);
    document.getElementById('createNewBtn').addEventListener('click', showEditor);
    document.getElementById('createNewWelcomeBtn').addEventListener('click', showEditor);
    
    // 文件列表操作
    document.getElementById('refreshListBtn').addEventListener('click', loadJsonFileList);
    document.getElementById('searchInput').addEventListener('input', filterFileList);
    
    // 编辑器操作
    document.getElementById('addModifierBtn').addEventListener('click', addModifierForm);
    document.getElementById('cancelEditBtn').addEventListener('click', cancelEdit);
    document.getElementById('previewJsonBtn').addEventListener('click', previewJson);
    document.getElementById('downloadJsonBtn').addEventListener('click', downloadJsonFromForm);
    document.getElementById('downloadFromPreviewBtn').addEventListener('click', downloadJsonFromPreview);
    
    // 查看器操作
    document.getElementById('editCurrentBtn').addEventListener('click', editCurrentJson);
    document.getElementById('downloadCurrentBtn').addEventListener('click', downloadCurrentJson);
}

// 加载JSON文件列表
async function loadJsonFileList() {
    const listElement = document.getElementById('jsonList');
    const loadingIndicator = document.getElementById('loadingIndicator');
    const emptyListMessage = document.getElementById('emptyListMessage');
    
    // 显示加载指示器
    loadingIndicator.style.display = 'block';
    listElement.style.display = 'none';
    emptyListMessage.style.display = 'none';
    
    try {
        // 这里需要根据你的实际仓库结构来获取文件列表
        // 方法1: 如果有一个存储文件列表的JSON文件
        // const response = await fetch('https://raw.githubusercontent.com/vitechliu/duckov-vtmodifiers/main/docs/data/filelist.json');
        // const fileList = await response.json();
        
        // 方法2: 手动列出已知文件（示例）
        // 在实际使用中，你可以通过GitHub API获取目录内容，但需要处理CORS
        // 这里我们使用一个示例文件列表
        const exampleFiles = [
            "default_0_5_1.json",
            "default_0_6_0.json",
            "default_0_7_0.json",
        ];
        
        // 模拟网络延迟
        await new Promise(resolve => setTimeout(resolve, 500));
        
        // 清空列表
        listElement.innerHTML = '';
        
        if (exampleFiles.length === 0) {
            loadingIndicator.style.display = 'none';
            emptyListMessage.style.display = 'block';
            return;
        }
        
        // 为每个文件创建列表项
        exampleFiles.forEach(filename => {
            const listItem = createFileListItem(filename);
            listElement.appendChild(listItem);
        });
        
        // 隐藏加载指示器，显示列表
        loadingIndicator.style.display = 'none';
        listElement.style.display = 'block';
        
    } catch (error) {
        console.error('加载文件列表失败:', error);
        loadingIndicator.style.display = 'none';
        listElement.innerHTML = `
            <div class="alert alert-danger m-3">
                <i class="bi bi-exclamation-triangle me-2"></i>
                无法加载文件列表。请检查网络连接或仓库配置。
            </div>
        `;
        listElement.style.display = 'block';
    }
}

// 创建文件列表项
function createFileListItem(filename) {
    const listItem = document.createElement('div');
    listItem.className = 'list-group-item list-group-item-action';
    listItem.dataset.filename = filename;
    
    listItem.innerHTML = `
        <div class="d-flex justify-content-between align-items-center">
            <div class="flex-grow-1">
                <h6 class="mb-1">
                    <i class="bi bi-file-earmark-json text-primary me-2"></i>
                    ${filename}
                </h6>
                <small class="text-muted file-info">点击查看详情</small>
            </div>
            <div>
                <button class="btn btn-sm btn-outline-primary view-file-btn" data-filename="${filename}">
                    <i class="bi bi-eye"></i>
                </button>
                <button class="btn btn-sm btn-outline-success download-file-btn ms-1" data-filename="${filename}">
                    <i class="bi bi-download"></i>
                </button>
            </div>
        </div>
    `;
    
    // 添加点击事件
    listItem.addEventListener('click', function(e) {
        if (!e.target.closest('button')) {
            viewJsonFile(filename);
        }
    });
    
    // 为按钮添加事件
    listItem.querySelector('.view-file-btn').addEventListener('click', function(e) {
        e.stopPropagation();
        viewJsonFile(filename);
    });
    
    listItem.querySelector('.download-file-btn').addEventListener('click', function(e) {
        e.stopPropagation();
        downloadJsonFile2(filename);
    });
    
    return listItem;
}

// 查看JSON文件
async function viewJsonFile(filename) {
    try {
        // 在实际使用中，这里应该从你的仓库获取文件
        const response = await fetch(`https://raw.githubusercontent.com/vitechliu/duckov-vtmodifiers/main/Resources/modifiers/${filename}`);
        const exampleData = await response.json();
        //
        // // 过滤出 JSON 文件
        // const jsonFiles = files
        //     .filter(file => file.name.endsWith('.json'))
        //     .map(file => file.name);
        // // 示例数据 - 使用你提供的样例
        // const exampleData = {
        //     "author": "Official",
        //     "version": "0.6.0",
        //     "isCommunity": false,
        //     "key": "default060",
        //     "modifiers": {
        //         "Feathering": {
        //             "key": "Feathering",
        //             "weight": 50,
        //             "quality": 5,
        //             "forceFixed": false,
        //             "applyOnGuns": false,
        //             "applyOnMelee": true,
        //             "applyOnEquipment": false,
        //             "applyOnHelmet": false,
        //             "applyOnArmor": true,
        //             "applyOnFaceMask": false,
        //             "applyOnBackpack": false,
        //             "data": {
        //                 "PriceMultiplier": 3,
        //                 "Moveability": 0.7
        //             }
        //         },
        //         "Heavy": {
        //             "key": "Heavy",
        //             "author": "Custom Author",
        //             "weight": 30,
        //             "quality": -2,
        //             "forceFixed": true,
        //             "applyOnGuns": true,
        //             "applyOnMelee": false,
        //             "applyOnEquipment": true,
        //             "applyOnHelmet": false,
        //             "applyOnArmor": true,
        //             "applyOnFaceMask": false,
        //             "applyOnBackpack": true,
        //             "data": {
        //                 "Weight": 1.5,
        //                 "Moveability": 0.5,
        //                 "PriceMultiplier": 0.8
        //             }
        //         }
        //     }
        // };
        
        // 模拟网络延迟
        await new Promise(resolve => setTimeout(resolve, 300));
        
        // 保存当前数据
        currentJsonData = exampleData;
        currentFileName = filename;
        isEditingExisting = false;
        
        // 显示查看器
        showJsonViewer(exampleData, filename);
        
        // 高亮当前选中的文件
        document.querySelectorAll('.list-group-item').forEach(item => {
            item.classList.remove('active');
            if (item.dataset.filename === filename) {
                item.classList.add('active');
            }
        });
        
    } catch (error) {
        console.error('加载JSON文件失败:', error);
        alert('无法加载文件: ' + filename);
    }
}

// 显示JSON查看器
function showJsonViewer(jsonData, filename) {
    // 隐藏其他内容
    document.getElementById('welcomeMessage').style.display = 'none';
    document.getElementById('jsonEditor').style.display = 'none';
    
    // 更新标题
    document.getElementById('fileNamePlaceholder').textContent = filename;
    
    // 渲染JSON内容
    renderJsonViewerContent(jsonData);
    
    // 显示查看器
    document.getElementById('jsonViewer').style.display = 'block';
}

// 渲染JSON查看器内容
function renderJsonViewerContent(jsonData) {
    const container = document.getElementById('jsonViewerContent');
    
    // 创建HTML内容
    let html = `
        <div class="mb-4">
            <h6>基本信息</h6>
            <table class="table table-bordered">
                <tr>
                    <th width="30%">Key</th>
                    <td>${jsonData.key}</td>
                </tr>
                <tr>
                    <th>作者</th>
                    <td>${jsonData.author}</td>
                </tr>
                <tr>
                    <th>版本</th>
                    <td>${jsonData.version}</td>
                </tr>
                <tr>
                    <th>是否社区内容</th>
                    <td>${jsonData.isCommunity ? '是' : '否'}</td>
                </tr>
            </table>
        </div>
        
        <div>
            <h6 class="d-flex justify-content-between align-items-center">
                <span>Modifiers (${Object.keys(jsonData.modifiers || {}).length})</span>
                <small class="text-muted">点击展开查看详情</small>
            </h6>
    `;
    
    // 添加每个modifier
    if (jsonData.modifiers && Object.keys(jsonData.modifiers).length > 0) {
        Object.entries(jsonData.modifiers).forEach(([modifierKey, modifier]) => {
            html += createModifierViewHtml(modifierKey, modifier);
        });
    } else {
        html += '<div class="alert alert-info">此 Group 不包含任何 Modifier。</div>';
    }
    
    html += '</div>';
    
    container.innerHTML = html;
    
    // 为折叠按钮添加事件
    container.querySelectorAll('.collapse-btn').forEach(btn => {
        btn.addEventListener('click', function() {
            const targetId = this.dataset.target;
            const target = document.getElementById(targetId);
            const icon = this.querySelector('i');
            
            if (target.classList.contains('show')) {
                icon.className = 'bi bi-chevron-down';
            } else {
                icon.className = 'bi bi-chevron-up';
            }
        });
    });
}

// 创建Modifier查看HTML
function createModifierViewHtml(modifierKey, modifier) {
    const collapseId = `modifier-${modifierCounter++}`;
    
    return `
        <div class="card mb-3">
            <div class="card-header bg-light-subtle d-flex justify-content-between align-items-center">
                <h6 class="mb-0">
                    <i class="bi bi-tag me-2"></i>
                    ${modifierKey}
                </h6>
                <button class="btn btn-sm btn-outline-secondary collapse-btn" type="button" data-bs-toggle="collapse" data-bs-target="#${collapseId}">
                    <i class="bi bi-chevron-down"></i>
                </button>
            </div>
            <div class="collapse show" id="${collapseId}">
                <div class="card-body">
                    <div class="row">
                        <div class="col-md-6">
                            <table class="table table-sm">
                                <tr>
                                    <th width="40%">Key</th>
                                    <td>${modifier.key}</td>
                                </tr>
                                <tr>
                                    <th>作者</th>
                                    <td>${modifier.author || '未指定'}</td>
                                </tr>
                                <tr>
                                    <th>权重</th>
                                    <td>${modifier.weight}</td>
                                </tr>
                                <tr>
                                    <th>等级</th>
                                    <td>${modifier.quality}</td>
                                </tr>
                                <tr>
                                    <th>强制固定</th>
                                    <td>${modifier.forceFixed ? '是' : '否'}</td>
                                </tr>
                            </table>
                        </div>
                        <div class="col-md-6">
                            <h6>应用目标</h6>
                            <div class="row">
                                ${createApplyTargetsHtml(modifier)}
                            </div>
                        </div>
                    </div>
                    
                    ${createModifierDataHtml(modifier.data)}
                </div>
            </div>
        </div>
    `;
}

// 创建应用目标HTML

const applyTargetsDict = [
    { key: 'applyOnGuns', label: '枪械' },
    { key: 'applyOnMelee', label: '近战武器' },
    { key: 'applyOnEquipment', label: '装备' },
    { key: 'applyOnHelmet', label: '头盔' },
    { key: 'applyOnArmor', label: '护甲' },
    { key: 'applyOnFaceMask', label: '面罩' },
    { key: 'applyOnHeadset', label: '耳机' },
    { key: 'applyOnBackpack', label: '背包' }
];
function createApplyTargetsHtml(modifier) {
    let html = '';
    applyTargetsDict.forEach(target => {
        if (modifier[target.key]) {
            html += `
                <div class="col-6 col-md-4">
                    <span class="badge bg-success">
                        <i class="bi bi-check-circle me-1"></i> ${target.label}
                    </span>
                </div>
            `;
        }
    });
    
    return html || '<div class="col-12"><span class="text-muted">未指定应用目标</span></div>';
}

// 创建Modifier Data HTML
function createModifierDataHtml(data) {
    if (!data || Object.keys(data).length === 0) {
        return '<div class="mt-3"><em>此 Modifier 没有 Data 字段。</em></div>';
    }
    
    let html = '<div class="mt-3"><h6>Data 字段</h6><div class="row">';
    
    Object.entries(data).forEach(([key, value], index) => {
        const keyChs = vtms[key] ?? key;
        html += `
            <div class="col-md-4 mb-2">
                <div class="border rounded p-2">
                    <div class="fw-bold">${keyChs}</div>
                    <div>${value}</div>
                </div>
            </div>
        `;
    });
    
    html += '</div></div>';
    return html;
}

// 显示文件列表
function showFileList() {
    document.getElementById('welcomeMessage').style.display = 'block';
    document.getElementById('jsonViewer').style.display = 'none';
    document.getElementById('jsonEditor').style.display = 'none';
}

// 显示编辑器
function showEditor() {
    // 重置表单
    resetEditorForm();
    
    // 更新标题
    document.getElementById('editorTitle').textContent = '创建新的 Modifier Group';
    
    // 隐藏其他内容
    document.getElementById('welcomeMessage').style.display = 'none';
    document.getElementById('jsonViewer').style.display = 'none';
    
    // 显示编辑器
    document.getElementById('jsonEditor').style.display = 'block';
    
    // 添加一个初始的modifier表单
    addModifierForm();
}

// 重置编辑器表单
function resetEditorForm() {
    document.getElementById('modifierGroupForm').reset();
    document.getElementById('modifiersContainer').innerHTML = `
        <div class="alert alert-info">
            点击"添加 Modifier"按钮开始添加第一个 Modifier。
        </div>
    `;
    
    // 重置计数器
    modifierCounter = 0;
    dataFieldCounter = 0;
    
    // 重置全局变量
    currentJsonData = null;
    currentFileName = null;
    isEditingExisting = false;
}

// 添加Modifier表单
function addModifierForm() {
    const container = document.getElementById('modifiersContainer');
    
    // 如果当前是提示信息，清除它
    if (container.querySelector('.alert-info')) {
        container.innerHTML = '';
    }
    
    // 克隆模板
    const template = document.getElementById('modifierTemplate');
    const newModifier = template.content.cloneNode(true);
    
    // 设置唯一ID
    const modifierId = `modifier-${modifierCounter++}`;
    newModifier.querySelector('.modifier-form').id = modifierId;
    
    // 更新标题
    const modifierKeyInput = newModifier.querySelector('.modifier-key');
    newModifier.querySelector('.modifier-title').textContent = `Modifier #${modifierCounter}`;
    
    const selectElement = newModifier.querySelector('#mainSelectVtms')
    fillSelect(selectElement);
    
    // 监听key输入变化
    modifierKeyInput.addEventListener('input', function() {
        const titleElement = document.querySelector(`#${modifierId} .modifier-title`);
        titleElement.textContent = this.value ? this.value : `Modifier #${modifierCounter}`;
    });
    
    // 绑定删除按钮事件
    newModifier.querySelector('.remove-modifier').addEventListener('click', function() {
        this.closest('.modifier-form').remove();
        
        // 如果没有modifier了，显示提示信息
        if (container.children.length === 0) {
            container.innerHTML = `
                <div class="alert alert-info">
                    点击"添加 Modifier"按钮开始添加第一个 Modifier。
                </div>
            `;
        }
    });
    
    // 绑定添加data字段按钮事件
    newModifier.querySelector('.add-data-field').addEventListener('click', function() {
        addDataField(this.closest('.modifier-form').querySelector('.data-fields'));
    });
    
    // 绑定data字段删除按钮事件（为初始字段）
    newModifier.querySelector('.remove-data-field').addEventListener('click', function() {
        this.closest('.data-field-item').remove();
    });
    
    // 绑定data字段key选择变化事件
    newModifier.querySelector('.data-key').addEventListener('change', function() {
        if (this.value === 'custom') {
            // 将select替换为input
            const customInput = document.createElement('input');
            customInput.type = 'text';
            customInput.className = 'form-control data-key-custom';
            customInput.placeholder = '输入自定义键名';
            customInput.required = true;
            
            this.parentNode.replaceChild(customInput, this);
        }
    });
    
    // 添加到容器
    container.appendChild(newModifier);
}

// 添加Data字段
function addDataField(container) {
    const fieldId = `data-field-${dataFieldCounter++}`;
    
    const fieldHtml = `
        <div class="data-field-item row g-2 mb-2" id="${fieldId}">
            <div class="col-md-5">
                <select class="form-select data-key">
                    <option value="">选择键...</option>
                    ${Object.keys(vtms).map(key => `<option value="${key}">${vtms[key]}</option>`).join('')}
                    <option value="custom">自定义...</option>
                </select>
            </div>
            <div class="col-md-5">
                <input type="number" class="form-control data-value" step="0.01" placeholder="数值" value="1.0">
            </div>
            <div class="col-md-2">
                <button type="button" class="btn btn-sm btn-outline-danger w-100 remove-data-field">
                    <i class="bi bi-trash"></i>
                </button>
            </div>
        </div>
    `;
    
    const tempDiv = document.createElement('div');
    tempDiv.innerHTML = fieldHtml;
    const fieldElement = tempDiv.firstElementChild;
    
    // 绑定删除按钮事件
    fieldElement.querySelector('.remove-data-field').addEventListener('click', function() {
        fieldElement.remove();
    });
    
    // 绑定key选择变化事件
    fieldElement.querySelector('.data-key').addEventListener('change', function() {
        if (this.value === 'custom') {
            // 将select替换为input
            const customInput = document.createElement('input');
            customInput.type = 'text';
            customInput.className = 'form-control data-key-custom';
            customInput.placeholder = '输入自定义键名';
            customInput.required = true;
            
            this.parentNode.replaceChild(customInput, this);
        }
    });
    
    container.appendChild(fieldElement);
}

// 过滤文件列表
function filterFileList() {
    const searchTerm = document.getElementById('searchInput').value.toLowerCase();
    const items = document.querySelectorAll('#jsonList .list-group-item');
    
    items.forEach(item => {
        const filename = item.dataset.filename.toLowerCase();
        const fileInfo = item.querySelector('.file-info').textContent.toLowerCase();
        
        if (filename.includes(searchTerm) || fileInfo.includes(searchTerm)) {
            item.style.display = 'block';
        } else {
            item.style.display = 'none';
        }
    });
}

// 取消编辑
function cancelEdit() {
    if (currentJsonData && currentFileName) {
        // 如果正在编辑现有文件，返回查看器
        showJsonViewer(currentJsonData, currentFileName);
    } else {
        // 否则返回欢迎页面
        showFileList();
    }
}

// 编辑当前JSON
function editCurrentJson() {
    if (!currentJsonData) return;
    
    // 设置编辑模式
    isEditingExisting = true;
    
    // 更新标题
    document.getElementById('editorTitle').textContent = `编辑: ${currentFileName}`;
    
    // 填充表单数据
    populateFormWithData(currentJsonData);
    
    // 隐藏查看器，显示编辑器
    document.getElementById('jsonViewer').style.display = 'none';
    document.getElementById('jsonEditor').style.display = 'block';
}

// 用现有数据填充表单
function populateFormWithData(jsonData) {
    // 填充基本信息
    document.getElementById('key').value = jsonData.key || '';
    document.getElementById('author').value = jsonData.author || 'Official';
    document.getElementById('version').value = jsonData.version || '0.0.1';
    document.getElementById('isCommunity').checked = jsonData.isCommunity || false;
    
    // 清空现有的modifiers
    const container = document.getElementById('modifiersContainer');
    container.innerHTML = '';
    
    // 添加每个modifier
    if (jsonData.modifiers && Object.keys(jsonData.modifiers).length > 0) {
        Object.values(jsonData.modifiers).forEach(modifier => {
            addModifierForm();
            
            // 获取刚刚添加的modifier表单
            const lastModifier = container.lastElementChild;
            
            // 填充modifier数据
            lastModifier.querySelector('.modifier-key').value = modifier.key || '';
            lastModifier.querySelector('[name="author"]').value = modifier.author || '';
            lastModifier.querySelector('[name="weight"]').value = modifier.weight || 0;
            lastModifier.querySelector('[name="quality"]').value = modifier.quality || 0;
            lastModifier.querySelector('[name="forceFixed"]').checked = modifier.forceFixed || false;
            
            // 填充应用目标
            const applyTargets = Object.keys(applyTargetsDict)
            
            applyTargets.forEach(target => {
                lastModifier.querySelector(`[name="${target}"]`).checked = modifier[target] || false;
            });
            
            // 填充data字段
            if (modifier.data && Object.keys(modifier.data).length > 0) {
                // 清除初始的data字段
                lastModifier.querySelector('.data-fields').innerHTML = '';
                
                // 添加每个data字段
                Object.entries(modifier.data).forEach(([key, value]) => {
                    const dataFieldsContainer = lastModifier.querySelector('.data-fields');
                    addDataField(dataFieldsContainer);
                    
                    // 获取刚刚添加的data字段
                    const lastDataField = dataFieldsContainer.lastElementChild;
                    
                    // 检查key是否是预定义的
                    if (vtms.hasOwnProperty(key)) {
                        lastDataField.querySelector('.data-key').value = key;
                    } else {
                        // 自定义key
                        lastDataField.querySelector('.data-key').value = 'custom';
                        
                        // 触发change事件以显示自定义输入
                        lastDataField.querySelector('.data-key').dispatchEvent(new Event('change'));
                        
                        // 设置自定义输入的值
                        setTimeout(() => {
                            const customInput = lastDataField.querySelector('.data-key-custom');
                            if (customInput) {
                                customInput.value = key;
                            }
                        }, 0);
                    }
                    
                    lastDataField.querySelector('.data-value').value = value;
                });
            }
        });
    } else {
        container.innerHTML = `
            <div class="alert alert-info">
                点击"添加 Modifier"按钮开始添加第一个 Modifier。
            </div>
        `;
        addModifierForm();
    }
}

// 从表单生成JSON
function generateJsonFromForm() {
    // 收集基本信息
    const modifierGroup = {
        key: document.getElementById('key').value,
        author: document.getElementById('author').value,
        version: document.getElementById('version').value,
        isCommunity: document.getElementById('isCommunity').checked,
        modifiers: {}
    };
    
    // 收集所有modifiers
    const modifierForms = document.querySelectorAll('.modifier-form');
    
    modifierForms.forEach(form => {
        const modifierKey = form.querySelector('.modifier-key').value;
        if (!modifierKey) return; // 跳过没有key的modifier
        
        const modifier = {
            key: modifierKey,
            author: form.querySelector('[name="author"]').value || null,
            weight: parseInt(form.querySelector('[name="weight"]').value) || 0,
            quality: parseInt(form.querySelector('[name="quality"]').value) || 0,
            forceFixed: form.querySelector('[name="forceFixed"]').checked,
            applyOnGuns: form.querySelector('[name="applyOnGuns"]').checked,
            applyOnMelee: form.querySelector('[name="applyOnMelee"]').checked,
            applyOnEquipment: form.querySelector('[name="applyOnEquipment"]').checked,
            applyOnHelmet: form.querySelector('[name="applyOnHelmet"]').checked,
            applyOnArmor: form.querySelector('[name="applyOnArmor"]').checked,
            applyOnFaceMask: form.querySelector('[name="applyOnFaceMask"]').checked,
            applyOnHeadset: form.querySelector('[name="applyOnHeadset"]').checked,
            applyOnBackpack: form.querySelector('[name="applyOnBackpack"]').checked,
            data: {}
        };
        
        // 收集data字段
        const dataFields = form.querySelectorAll('.data-field-item');
        dataFields.forEach(field => {
            let keyInput = field.querySelector('.data-key');
            if (!keyInput)
                keyInput = field.querySelector('.data-key-custom');
            let keyValue;
            
            // 处理自定义key
            if (keyInput.tagName === 'SELECT') {
                keyValue = keyInput.value;
            } else {
                keyValue = keyInput.value;
            }
            
            const valueInput = field.querySelector('.data-value');
            const valueValue = parseFloat(valueInput.value) || 0;
            
            if (keyValue && keyValue !== 'custom') {
                modifier.data[keyValue] = valueValue;
            }
        });
        
        // 添加到modifiers对象
        modifierGroup.modifiers[modifierKey] = modifier;
    });
    
    return modifierGroup;
}

// 预览JSON
function previewJson() {
    // 验证表单
    if (!document.getElementById('key').value) {
        alert('请填写 Modifier Group 的 Key 字段');
        document.getElementById('key').focus();
        return;
    }
    
    // 检查是否有modifier
    const modifierForms = document.querySelectorAll('.modifier-form');
    if (modifierForms.length === 0) {
        alert('请至少添加一个 Modifier');
        return;
    }
    
    // 检查每个modifier的key
    let hasMissingKey = false;
    modifierForms.forEach(form => {
        if (!form.querySelector('.modifier-key').value) {
            hasMissingKey = true;
            form.querySelector('.modifier-key').focus();
        }
    });
    
    if (hasMissingKey) {
        alert('请填写所有 Modifier 的 Key 字段');
        return;
    }
    
    // 生成JSON
    const jsonData = generateJsonFromForm();
    
    // 格式化JSON
    const formattedJson = JSON.stringify(jsonData, null, 2);
    
    // 显示预览
    document.getElementById('jsonPreviewCode').textContent = formattedJson;
    
    // 高亮代码
    hljs.highlightElement(document.getElementById('jsonPreviewCode'));
    
    // 显示模态框
    const modal = new bootstrap.Modal(document.getElementById('jsonPreviewModal'));
    modal.show();
    
    // 保存当前数据
    currentJsonData = jsonData;
    currentFileName = `${jsonData.key}.json`;
}

// 从表单下载JSON
function downloadJsonFromForm() {
    // 验证表单
    if (!document.getElementById('key').value) {
        alert('请填写 Modifier Group 的 Key 字段');
        document.getElementById('key').focus();
        return;
    }
    
    // 生成JSON
    const jsonData = generateJsonFromForm();
    
    // 下载
    downloadJsonFile1(jsonData, `${jsonData.key}.json`);
}

// 从预览下载JSON
function downloadJsonFromPreview() {
    if (currentJsonData) {
        downloadJsonFile1(currentJsonData, currentFileName || `${currentJsonData.key}.json`);
        
        // 关闭模态框
        const modal = bootstrap.Modal.getInstance(document.getElementById('jsonPreviewModal'));
        modal.hide();
    }
}

// 下载当前查看的JSON
function downloadCurrentJson() {
    if (currentJsonData && currentFileName) {
        downloadJsonFile1(currentJsonData, currentFileName);
    }
}

// 下载JSON文件（通用函数）
function downloadJsonFile1(jsonData, filename) {
    // 创建JSON字符串
    const dataStr = JSON.stringify(jsonData, null, 2);
    
    // 创建Blob
    const blob = new Blob([dataStr], { type: 'application/json' });
    
    // 创建下载链接
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    
    // 触发下载
    document.body.appendChild(link);
    link.click();
    
    // 清理
    setTimeout(() => {
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
    }, 100);
    
    // 显示成功消息
    showNotification(`已下载: ${filename}`, 'success');
}

// 下载示例JSON文件
async function downloadJsonFile2(filename) {
    try {
        // 在实际使用中，这里应该从你的仓库获取文件
        // const response = await fetch(`https://raw.githubusercontent.com/vitechliu/duckov-vtmodifiers/main/Resources/modifiers/${filename}`);
        
        // 示例数据
        const exampleData = {
            "author": "Official",
            "version": "0.6.0",
            "isCommunity": false,
            "key": filename.replace('.json', ''),
            "modifiers": {
                "ExampleModifier": {
                    "key": "ExampleModifier",
                    "weight": 50,
                    "quality": 5,
                    "forceFixed": false,
                    "applyOnGuns": true,
                    "applyOnMelee": false,
                    "applyOnEquipment": true,
                    "applyOnHelmet": false,
                    "applyOnArmor": false,
                    "applyOnFaceMask": false,
                    "applyOnHeadset": false,
                    "applyOnBackpack": false,
                    "data": {
                        "PriceMultiplier": 1.5,
                        "Damage": 1.2
                    }
                }
            }
        };
        
        // 下载文件
        downloadJsonFile1(exampleData, filename);
        
    } catch (error) {
        console.error('下载文件失败:', error);
        alert('无法下载文件: ' + filename);
    }
}

// 显示通知
function showNotification(message, type = 'info') {
    // 创建通知元素
    const notification = document.createElement('div');
    notification.className = `alert alert-${type} alert-dismissible fade show position-fixed`;
    notification.style.cssText = `
        top: 20px;
        right: 20px;
        z-index: 9999;
        min-width: 300px;
    `;
    notification.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    
    // 添加到页面
    document.body.appendChild(notification);
    
    // 自动移除
    setTimeout(() => {
        notification.remove();
    }, 3000);
}
