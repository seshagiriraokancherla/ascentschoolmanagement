package com.ascentschools.mobile.ui.school

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import com.ascentschools.mobile.ui.theme.NavyBlue

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SchoolCodeScreen(
    viewModel: SchoolCodeViewModel,
    onResolved: () -> Unit
) {
    val state by viewModel.state.collectAsState()
    var code by remember { mutableStateOf("") }

    LaunchedEffect(state) {
        if (state is SchoolCodeState.Done) onResolved()
    }

    Box(
        modifier = Modifier.fillMaxSize().padding(24.dp),
        contentAlignment = Alignment.Center
    ) {
        Card(modifier = Modifier.fillMaxWidth()) {
            Column(
                modifier = Modifier.padding(24.dp),
                horizontalAlignment = Alignment.CenterHorizontally
            ) {
                Text("CHAK IN", style = MaterialTheme.typography.headlineSmall, color = NavyBlue)
                Spacer(Modifier.height(8.dp))
                Text(
                    "Enter the 4-digit school code shared by your school",
                    style = MaterialTheme.typography.bodyMedium,
                    textAlign = TextAlign.Center
                )
                Spacer(Modifier.height(20.dp))

                val confirm = state as? SchoolCodeState.Confirm
                if (confirm != null) {
                    Text(
                        confirm.school.name ?: "School",
                        style = MaterialTheme.typography.titleMedium,
                        textAlign = TextAlign.Center
                    )
                    Spacer(Modifier.height(4.dp))
                    Text("Is this your school?", style = MaterialTheme.typography.bodySmall)
                    Spacer(Modifier.height(20.dp))
                    Row(horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                        OutlinedButton(onClick = { viewModel.reset() }) { Text("Change") }
                        Button(
                            onClick = { viewModel.confirm(confirm.school) },
                            colors = ButtonDefaults.buttonColors(containerColor = NavyBlue)
                        ) { Text("Confirm") }
                    }
                } else {
                    OutlinedTextField(
                        value = code,
                        onValueChange = { v -> if (v.length <= 4 && v.all { it.isDigit() }) code = v },
                        label = { Text("School code") },
                        singleLine = true,
                        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                        modifier = Modifier.fillMaxWidth()
                    )
                    (state as? SchoolCodeState.Error)?.let {
                        Spacer(Modifier.height(8.dp))
                        Text(
                            it.message,
                            color = MaterialTheme.colorScheme.error,
                            style = MaterialTheme.typography.bodySmall
                        )
                    }
                    Spacer(Modifier.height(20.dp))
                    Button(
                        onClick = { viewModel.resolve(code) },
                        enabled = state !is SchoolCodeState.Loading,
                        modifier = Modifier.fillMaxWidth(),
                        colors = ButtonDefaults.buttonColors(containerColor = NavyBlue)
                    ) {
                        if (state is SchoolCodeState.Loading) {
                            CircularProgressIndicator(
                                modifier = Modifier.size(20.dp),
                                color = Color.White,
                                strokeWidth = 2.dp
                            )
                        } else {
                            Text("Continue")
                        }
                    }
                }
            }
        }
    }
}
