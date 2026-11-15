let token = sessionStorage.getItem("token") || "";
let currentUser = null;
let productsApi = "http://localhost:5082/api/v1";
let page = 1;
let usersPage = 1;
const message = document.getElementById("message");

async function run(action) {
    message.textContent = "";
    try {
        await action();
    } catch (error) {
        message.textContent = error.message;
    }
}

async function api(path, method = "GET", body = null) {
    const url = path.startsWith("/products") ? productsApi + path : "/api/v1" + path;
    const headers = {};
    if (token) headers.Authorization = "Bearer " + token;
    if (body !== null) headers["Content-Type"] = "application/json";

    let response;
    try {
        response = await fetch(url, { method, headers, body: body === null ? null : JSON.stringify(body) });
    } catch {
        throw new Error("Не удалось связаться с сервером. Проверьте, что оба сервиса запущены.");
    }

    const text = await response.text();
    const data = text ? JSON.parse(text) : null;
    if (!response.ok) {
        if (response.status === 401 && !path.startsWith("/auth/")) {
            clearLogin();
            await showPage("account");
            throw new Error("Войдите снова: аккаунт или сеанс недоступен.");
        }
        const details = data?.errors ? Object.values(data.errors).flat().join(" ") : data?.detail || data?.title;
        throw new Error(details || "Ошибка запроса: " + response.status);
    }
    return data;
}

function updateNavigation() {
    document.getElementById("current-user").textContent = currentUser ? currentUser.name + " (" + currentUser.email + ")" : "Вход не выполнен";
    document.getElementById("account-button").hidden = !!currentUser;
    document.getElementById("mine-button").hidden = !currentUser;
    document.getElementById("profile-button").hidden = !currentUser;
    document.getElementById("logout-button").hidden = !currentUser;
    document.getElementById("admin-button").hidden = currentUser?.role !== "Admin";
}

function clearLogin() {
    token = "";
    currentUser = null;
    sessionStorage.removeItem("token");
    clearProductForm();
    document.getElementById("mine-list").replaceChildren();
    document.getElementById("users-list").replaceChildren();
    updateNavigation();
}

async function showPage(id) {
    if (["mine", "profile", "admin"].includes(id) && !token) id = "account";
    document.querySelectorAll("main > section").forEach(section => section.hidden = section.id !== id);
    if (id === "catalog") await loadCatalog();
    if (id === "mine") await loadMine();
    if (id === "profile") await loadProfile();
    if (id === "admin") await loadUsers();
}

function formData(form) {
    return Object.fromEntries(new FormData(form));
}

function onSubmit(id, action) {
    document.getElementById(id).addEventListener("submit", async event => {
        event.preventDefault();
        const button = event.submitter;
        if (button) button.disabled = true;
        try {
            await run(() => action(event.target));
        } finally {
            if (button) button.disabled = false;
        }
    });
}

function addButton(parent, text, action) {
    const button = document.createElement("button");
    button.type = "button";
    button.textContent = text;
    button.onclick = () => run(action);
    parent.append(" ", button);
}

function productItem(product) {
    const item = document.createElement("li");
    item.textContent = product.name + " — " + product.price.toFixed(2) + ". " + product.description +
        (product.isAvailable ? " (Есть в наличии)" : " (Нет в наличии)");
    return item;
}

function clearProductForm() {
    const form = document.getElementById("product-form");
    form.reset();
    form.elements.id.value = "";
}

async function loadCatalog() {
    const query = new URLSearchParams();
    for (const [key, value] of new FormData(document.getElementById("search-form"))) {
        if (value !== "") query.set(key, value);
    }
    query.set("page", page);
    query.set("pageSize", 20);
    const result = await api("/products?" + query);
    const list = document.getElementById("catalog-list");
    list.replaceChildren(...result.items.map(productItem));
    document.getElementById("catalog-total").textContent = result.total ? "Найдено товаров: " + result.total : "Товары не найдены.";
    document.getElementById("page-number").textContent = "Страница " + page;
    document.getElementById("previous-page").disabled = page <= 1;
    document.getElementById("next-page").disabled = page * 20 >= result.total;
}

async function loadMine() {
    const products = await api("/products/mine");
    const list = document.getElementById("mine-list");
    list.replaceChildren();
    document.getElementById("mine-total").textContent = products.length ? "Товаров: " + products.length : "У вас пока нет товаров.";
    for (const product of products) {
        const item = productItem(product);
        addButton(item, "Изменить", () => {
            const form = document.getElementById("product-form");
            form.elements.id.value = product.id;
            form.elements.name.value = product.name;
            form.elements.description.value = product.description;
            form.elements.price.value = product.price;
            form.elements.isAvailable.checked = product.isAvailable;
            form.elements.name.focus();
        });
        addButton(item, "Удалить", async () => {
            if (!confirm("Удалить товар «" + product.name + "»?")) return;
            await api("/products/" + product.id, "DELETE");
            clearProductForm();
            await loadMine();
            message.textContent = "Товар удалён.";
        });
        list.append(item);
    }
}

async function loadProfile() {
    currentUser = await api("/users/me");
    updateNavigation();
    document.getElementById("profile-info").textContent = currentUser.email + "; роль: " + currentUser.role +
        "; почта подтверждена: " + (currentUser.emailConfirmed ? "да" : "нет");
    document.getElementById("profile-form").elements.name.value = currentUser.name;
}

async function loadUsers() {
    const users = await api("/users?page=" + usersPage + "&pageSize=20");
    const list = document.getElementById("users-list");
    list.replaceChildren();
    for (const user of users) {
        const item = document.createElement("li");
        item.textContent = user.name + " — " + user.email + " — " + user.role + (user.isActive ? " (Активен)" : " (Отключён)");
        if (user.id !== currentUser.id) {
            addButton(item, user.isActive ? "Деактивировать" : "Активировать", async () => {
                await api("/users/" + user.id + (user.isActive ? "/deactivate" : "/activate"), "POST");
                await loadUsers();
                message.textContent = "Состояние пользователя изменено.";
            });
        }
        list.append(item);
    }
    document.getElementById("users-page-number").textContent = "Страница " + usersPage;
    document.getElementById("previous-users").disabled = usersPage <= 1;
    document.getElementById("next-users").disabled = users.length < 20;
}

onSubmit("login-form", async form => {
    const result = await api("/auth/login", "POST", formData(form));
    token = result.accessToken;
    sessionStorage.setItem("token", token);
    currentUser = await api("/users/me");
    updateNavigation();
    form.reset();
    await showPage("mine");
});

onSubmit("register-form", async form => {
    const data = formData(form);
    await api("/auth/register", "POST", data);
    document.getElementById("login-form").elements.email.value = data.email;
    document.getElementById("confirmation-form").elements.email.value = data.email;
    form.reset();
    message.textContent = "Аккаунт создан. Откройте ссылку в письме подтверждения, затем войдите.";
});

onSubmit("confirmation-form", async form => {
    await api("/auth/resend-confirmation", "POST", formData(form));
    message.textContent = "Если аккаунт ожидает подтверждения, письмо отправлено. Проверьте почту.";
});

onSubmit("forgot-form", async form => {
    await api("/auth/forgot-password", "POST", formData(form));
    message.textContent = "Если аккаунт доступен, ссылка восстановления отправлена. Проверьте почту.";
});

onSubmit("reset-form", async form => {
    await api("/auth/reset-password", "POST", formData(form));
    form.reset();
    clearLogin();
    await showPage("account");
    message.textContent = "Пароль изменён. Войдите с новым паролем.";
});

onSubmit("product-form", async form => {
    const data = formData(form);
    const body = { name: data.name, description: data.description, price: Number(data.price), isAvailable: form.elements.isAvailable.checked };
    await api(data.id ? "/products/" + data.id : "/products", data.id ? "PUT" : "POST", body);
    clearProductForm();
    await loadMine();
    message.textContent = "Товар сохранён.";
});

onSubmit("profile-form", async form => {
    await api("/users/" + currentUser.id, "PUT", formData(form));
    await loadProfile();
    message.textContent = "Имя сохранено.";
});

onSubmit("search-form", async () => { page = 1; await loadCatalog(); });
document.querySelectorAll("[data-page]").forEach(button => button.onclick = () => run(() => showPage(button.dataset.page)));
document.getElementById("previous-page").onclick = () => run(async () => { page--; await loadCatalog(); });
document.getElementById("next-page").onclick = () => run(async () => { page++; await loadCatalog(); });
document.getElementById("previous-users").onclick = () => run(async () => { usersPage--; await loadUsers(); });
document.getElementById("next-users").onclick = () => run(async () => { usersPage++; await loadUsers(); });
document.getElementById("refresh-users").onclick = () => run(loadUsers);
document.getElementById("cancel-edit").onclick = clearProductForm;
document.getElementById("logout-button").onclick = () => run(async () => { clearLogin(); await showPage("catalog"); });
document.getElementById("deactivate-button").onclick = () => run(async () => {
    if (!confirm("Деактивировать аккаунт? Товары будут скрыты. Для активации понадобится администратор.")) return;
    await api("/users/" + currentUser.id + "/deactivate", "POST");
    clearLogin();
    await showPage("catalog");
    message.textContent = "Аккаунт деактивирован.";
});

run(async () => {
    const configuration = await fetch("/site-config").then(response => response.json());
    productsApi = configuration.productsApiUrl.replace(/\/$/, "");
    if (token) {
        try { currentUser = await api("/users/me"); } catch { clearLogin(); }
    }
    updateNavigation();

    const parameters = new URLSearchParams(location.search);
    const action = parameters.get("action");
    const emailToken = parameters.get("token");
    if (action) history.replaceState(null, "", location.pathname);
    if (action === "reset") {
        await showPage("reset");
        document.getElementById("reset-form").elements.token.value = emailToken || "";
        message.textContent = emailToken ? "Введите новый пароль." : "В ссылке отсутствует код восстановления.";
    } else if (action === "confirm") {
        await showPage("account");
        await api("/auth/confirm-email?token=" + encodeURIComponent(emailToken || ""));
        message.textContent = "Почта подтверждена. Теперь можно войти.";
    } else {
        await showPage("catalog");
    }
});
