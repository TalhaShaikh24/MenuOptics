✅ Usage Examples Global ajaxRequest Function

🔹 GET with Query Parameters (Default Behavior)
ajaxRequest({
  url: '/api/search',
  method: 'GET',
  data: { q: 'laptop' } // ✅ Goes to URL as ?q=laptop
});

🔹 GET without Data (No query string)
ajaxRequest({
  url: '/api/products',
  method: 'GET'
  // ✅ No ? added, clean URL
});

🔹 DELETE with Body (sendInBody = true)
ajaxRequest({
  url: '/api/delete-user',
  method: 'DELETE',
  data: { userId: 123 },
  sendInBody: true, // ✅ Sends JSON body
  contentType: 'application/json',
});


🔹 POST with JSON (Normal case)
ajaxRequest({
  url: '/api/create',
  method: 'POST',
  data: { name: 'John', role: 'admin' },
});


🔹 POST with FormData
const formData = new FormData();
formData.append('file', $('#upload')[0].files[0]);

ajaxRequest({
  url: '/api/upload',
  method: 'POST',
  data: formData // ✅ Automatically handled as FormData
});




