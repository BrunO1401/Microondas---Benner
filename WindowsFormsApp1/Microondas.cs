using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Microondas : Form
    {
        // Cria a conexão com o BD SQL Server.
        private string connectionString = "Server=localhost\\SQLEXPRESS;Database=ListaPrograma;Trusted_Connection=True;TrustServerCertificate=True;";

        private int totalTime = 0; // Tempo total em segundos
        private System.Windows.Forms.Timer timer;

        private int segMax = 120; // Parâmentro para o tempo máximo.
        private int tempoLigar = 30; // Parâmetro para tempo inícial do botão Ligar.

        private int tempoMax = 120; // Parâmetro representa o tempo máximo do Micro-ondas.
        private int tempoMin = 0; // Parâmetro representa o tempo mínimo do Micro-ondas.

        private int contaClickPause = 0; // Contador do botão Pausa, para saber se é para Pausar / Cancelar / Apagar as informações em tela.

        private bool modoPrograma = false; // Parâmetro para identificar se oque está rodando é um programa pré-definido ou foi informado valores manualmente.

        private Dictionary<string, ListaPrograma> listaPrograma; // Declara um dicionário das informações das Listas de programas (Lista referente ao Banco de Dados).
        private Dictionary<string, ListaPrograma> listaProgramaFixos; // Declara um dicionário das informações das Listas de programas (Lista referente a Lista Fixa).

        private bool inicializandoComboBox = true; // Identificador da incialização do ComboBox.

        private char caracterAquecimento = '.'; // Parâmetro para setar o valor do char de Aquecimento padrão.

        private int tempoFinalizado = 0; // Identifica quando o tempo foi conclúido.

        public Microondas()
        {
            InitializeComponent();

            // Configura o Timer.
            timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000; // 1 segundo
            timer.Tick += Timer_Tick;

            // Configura o TrackBar de Potencia.
            trackBarPower.Minimum = 0;
            trackBarPower.Maximum = 15;
            trackBarPower.Value = 10;
            trackBarPower.TickFrequency = 10;
            trackBarPower.Scroll += trackBarPower_Scroll;

            // Configura o tamanho do Label do Aquecimento.
            lblAquecimento.MaximumSize = new Size(600, 0);
            lblAquecimento.AutoSize = true;

            // Inicializa os programas de aquecimento.
            InicializaPrgramaAquecimentoFixos();

            // Configura o ComboBox com os programas.
            CarregarProgramas();
            cmbProgramas.DataSource = new BindingSource(listaProgramaFixos, null);
            cmbProgramas.DisplayMember = "Key";
            cmbProgramas.ValueMember = "Value";

            cmbProgramas.SelectedIndex = -1;
            inicializandoComboBox = false;

            // Adiciona o evento de seleção de programa
            cmbProgramas.SelectedIndexChanged += cmbProgramas_SelectedIndexChanged;

            UpdateTimeDisplay();
        }

        private void btnLigar_Click(object sender, EventArgs e)
        {
            // Desativa o trackBar e o ComboBox quando o botão é clicado.
            trackBarPower.Enabled = false; // Desabilita a Barra da potência para ser alterada.
            cmbProgramas.Enabled = false; // Desabilita o comboBox de Programas a ser selecionado.

            if (tempoFinalizado > 0)
            {
                lblAquecimento.Text = string.Empty;
            }

            // Limpa o texto de lblAquecimento se o botão de pausa foi clicado mais de uma vez.
            if (contaClickPause > 1)
            {
                lblAquecimento.Text = string.Empty;
                contaClickPause = 0; // Reseta o valor da variável de Pausa para zero.
            }

            // Verifica e ajusta a potência se estiver em valores inválidos.
            if (trackBarPower.Value == 0)
            {
                trackBarPower.Value = 10;
                lblPotencia.Text = trackBarPower.Value.ToString();
            }
            else if (trackBarPower.Value > 10)
            {
                MessageBox.Show("Potência inválida."); // Informa que a potencia selecionada é inválida.
                trackBarPower.Enabled = true; // Habilita a Barra da potência para ser alterada.
                cmbProgramas.Enabled = true; // Habilita o comboBox de Programas a ser selecionado.
                return;
            }

            // Atualiza totalTime com base no texto do txtTemporizador.
            if (int.TryParse(txtTemporizador.Text, out int timeFromTextBox))
            {
                if (timeFromTextBox < tempoMin || timeFromTextBox > tempoMax) // Valida se o tempo selecionado passe do valor permitido.
                {
                    MessageBox.Show("O tempo digitado está fora do intervalo permitido."); // Informe em tela.
                    trackBarPower.Enabled = true; // Habilita a Barra da potência para ser alterada.
                    cmbProgramas.Enabled = true; // Habilita o comboBox de Programas a ser selecionado.
                    return;
                }
                totalTime = timeFromTextBox; // Tempo total toma o valor digitado no temporizador.
            }

            // Se o timer está ligado e não é um programa de aquecimento, adiciona 30 segundos ao tempo total.
            if (timer.Enabled && !modoPrograma)
            {
                if (totalTime + 30 <= tempoMax) // Valida se adicionando 30 segundos passa do tempo máximo.
                {
                    totalTime += 30; // Adiciona 30 segundos.
                }
                else
                {
                    totalTime = tempoMax; // Limita o temporizador no tempo máximo.
                }
            }
            else
            {
                // Se o timer não está ligado, inicializa o tempo se estiver zero.
                if (totalTime == 0)
                {
                    totalTime = 30; // Tempo total, toma o valor de 30 segundos.
                }
                else if (totalTime < tempoMin) // Valida se o tempo não está abaixo do tempo minimo.
                {
                    MessageBox.Show("O tempo está inválido.");
                    trackBarPower.Enabled = true; // Habilita a Barra da potência para ser alterada.
                    cmbProgramas.Enabled = true; // Habilita o comboBox de Programas a ser selecionado.
                    return;
                }
            }

            // Verifica se o tempo é válido para modos não-programa de aquecimento.
            if (!modoPrograma)
            {
                if (totalTime < tempoMin || totalTime > tempoMax) // Caso seja um programa de aquecimento, o tempo não tem limite, caso contrário irá entrar nessa condição.
                {
                    MessageBox.Show("Tempo inválido."); // Mensagem que irá aparecer em tela.
                    trackBarPower.Enabled = true; // Habilita a Barra da potência para ser alterada.
                    cmbProgramas.Enabled = true; // Habilita o comboBox de Programas a ser selecionado.
                    return;
                }
            }

            // Desativa os botões numéricos se estiver no modo de programa.
            if (modoPrograma) // Para que não possa ser possivel acrescentar nenhum tempo durante um programa de aquecimento.
            {
                // Desabilita os botões numéricos.
                btnUm.Enabled = false;
                btnDois.Enabled = false;
                btnTres.Enabled = false;
                btnQuatro.Enabled = false;
                btnCinco.Enabled = false;
                btnSeis.Enabled = false;
                btnSete.Enabled = false;
                btnOito.Enabled = false;
                btnNove.Enabled = false;
                btnZero.Enabled = false;
            }
            // Inicia o timer.
            timer.Start();
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            if (timer.Enabled) // Valida se o timer está rodando.
            {
                // Se o timer está rodando
                timer.Stop(); // Parar o temporizador.
                contaClickPause = 1; // tomar 1 ponto na variável de pause.
            }
            else
            {
                contaClickPause++; // Adiciona 1 sempre que o botão for prescinado.

                if (contaClickPause > 1)
                {
                    // Se o timer não está rodando e foi clicado mais de uma vez.
                    timer.Stop(); // Para o timer.
                    totalTime = 0; // Timer toma como 0.
                    lblAquecimento.Text = string.Empty; // Limpa string de Aquecimento.
                    trackBarPower.Value = 10; // Volta para o valor padrão de aquecimento.
                    lblPotencia.Text = trackBarPower.Value.ToString(); // Label que sinaliza a Potência toma o novo valor.
                    UpdateTimeDisplay(); // Atualiza o Timer.
                    contaClickPause = 0; // Reseta variável de contagem de pausa a zero.
                    trackBarPower.Enabled = true; // Habilita a barra de potência a ser alterada.
                    cmbProgramas.Enabled = true; // Habilita o comboBox de programa de aquecimento a ser alterada.
                    modoPrograma = false; // Reseta a variável de identificador de programa de aquecimento a false.
                    cmbProgramas.SelectedItem = null; // limpa a seleção do comboBox.
                    txtTemporizador.Enabled = true; // Habilita poder alterar o temporizador.

                    // Reabilita todos os botões numéricos
                    btnUm.Enabled = true;
                    btnDois.Enabled = true;
                    btnTres.Enabled = true;
                    btnQuatro.Enabled = true;
                    btnCinco.Enabled = true;
                    btnSeis.Enabled = true;
                    btnSete.Enabled = true;
                    btnOito.Enabled = true;
                    btnNove.Enabled = true;
                    btnZero.Enabled = true;
                }
                else
                {
                    // Se o timer não está rodando e é o primeiro clique
                    timer.Stop(); // Para o timer.
                    totalTime = 0; // Timer toma como 0.
                    lblAquecimento.Text = string.Empty; // Limpa string de Aquecimento.
                    trackBarPower.Value = 10; // Volta para o valor padrão de aquecimento.
                    lblPotencia.Text = trackBarPower.Value.ToString(); // Label que sinaliza a Potência toma o novo valor.
                    UpdateTimeDisplay(); // Atualiza o Timer.
                    contaClickPause = 0; // Reseta a contagem de cliques de pausa
                    trackBarPower.Enabled = true; // Habilita a barra de potência a ser alterada.
                    cmbProgramas.Enabled = true; // Habilita o comboBox de programa de aquecimento a ser alterada.
                    modoPrograma = false; // Reseta a variável de identificador de programa de aquecimento a false.
                    cmbProgramas.SelectedItem = null; // limpa a seleção do comboBox.
                    txtTemporizador.Enabled = true; // Habilita poder alterar o temporizador.

                    // Reabilita todos os botões numéricos
                    btnUm.Enabled = true;
                    btnDois.Enabled = true;
                    btnTres.Enabled = true;
                    btnQuatro.Enabled = true;
                    btnCinco.Enabled = true;
                    btnSeis.Enabled = true;
                    btnSete.Enabled = true;
                    btnOito.Enabled = true;
                    btnNove.Enabled = true;
                    btnZero.Enabled = true;
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            cadastroPrograma cadastroPrograma = new cadastroPrograma();

            cadastroPrograma.eventoFechar += (S, args) => AtualizarComboBox(); // Recebe evento de fechar o formulário para que o comboBox de programa de aquecimento seja atualizado.

            cadastroPrograma.Show(); // Abre a Tela de cadastro de novos programas de aquecimento.
        }

        private void btnUm_Click(object sender, EventArgs e)
        {
            // Adiciona 1 ao tempo total e atualiza o temporizador.
            if (int.TryParse(((Button)sender).Text, out int number))
            {
                int newTime = totalTime * 10 + number;

                if (newTime <= segMax)
                {
                    totalTime = newTime;
                    UpdateTimeDisplay();
                }
            }
        }

        private void btnDois_Click(object sender, EventArgs e)
        {
            // Adiciona 2 ao tempo total e atualiza o temporizador.
            if (int.TryParse(((Button)sender).Text, out int number))
            {
                int newTime = totalTime * 10 + number;

                if (newTime <= segMax)
                {
                    totalTime = newTime;
                    UpdateTimeDisplay();
                }
            }
        }

        private void btnTres_Click(object sender, EventArgs e)
        {
            // Adiciona 3 ao tempo total e atualiza o temporizador.
            if (int.TryParse(((Button)sender).Text, out int number))
            {
                int newTime = totalTime * 10 + number;

                if (newTime <= segMax)
                {
                    totalTime = newTime;
                    UpdateTimeDisplay();
                }
            }
        }

        private void btnQuatro_Click(object sender, EventArgs e)
        {
            // Adiciona 4 ao tempo total e atualiza o temporizador.
            if (int.TryParse(((Button)sender).Text, out int number))
            {
                int newTime = totalTime * 10 + number;

                if (newTime <= segMax)
                {
                    totalTime = newTime;
                    UpdateTimeDisplay();
                }
            }
        }

        private void btnCinco_Click(object sender, EventArgs e)
        {
            // Adiciona 5 ao tempo total e atualiza o temporizador.
            if (int.TryParse(((Button)sender).Text, out int number))
            {
                int newTime = totalTime * 10 + number;

                if (newTime <= segMax)
                {
                    totalTime = newTime;
                    UpdateTimeDisplay();
                }
            }
        }

        private void btnSeis_Click(object sender, EventArgs e)
        {
            // Adiciona 6 ao tempo total e atualiza o temporizador.
            if (int.TryParse(((Button)sender).Text, out int number))
            {
                int newTime = totalTime * 10 + number;

                if (newTime <= segMax)
                {
                    totalTime = newTime;
                    UpdateTimeDisplay();
                }
            }
        }

        private void btnSete_Click(object sender, EventArgs e)
        {
            // Adiciona 7 ao tempo total e atualiza o temporizador.
            if (int.TryParse(((Button)sender).Text, out int number))
            {
                int newTime = totalTime * 10 + number;

                if (newTime <= segMax)
                {
                    totalTime = newTime;
                    UpdateTimeDisplay();
                }
            }
        }

        private void btnOito_Click(object sender, EventArgs e)
        {
            // Adiciona 8 ao tempo total e atualiza o temporizador.
            if (int.TryParse(((Button)sender).Text, out int number))
            {
                int newTime = totalTime * 10 + number;

                if (newTime <= segMax)
                {
                    totalTime = newTime;
                    UpdateTimeDisplay();
                }
            }
        }

        private void btnNove_Click(object sender, EventArgs e)
        {
            // Adiciona 9 ao tempo total e atualiza o temporizador.
            if (int.TryParse(((Button)sender).Text, out int number))
            {
                int newTime = totalTime * 10 + number;

                if (newTime <= segMax)
                {
                    totalTime = newTime;
                    UpdateTimeDisplay();
                }
            }
        }

        private void btnZero_Click(object sender, EventArgs e)
        {
            // Adiciona 0 ao tempo total e atualiza o temporizador.
            if (int.TryParse(((Button)sender).Text, out int number))
            {
                int newTime = totalTime * 10 + number;

                if (newTime <= segMax)
                {
                    totalTime = newTime;
                    UpdateTimeDisplay();
                }
            }
        }

        private void lblPotencia_Click(object sender, EventArgs e)
        {
            lblPotencia.Text = trackBarPower.Value.ToString();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Estiliza o comboBox dos programas de aquecimento.
            cmbProgramas.DrawMode = DrawMode.OwnerDrawFixed;
            cmbProgramas.DrawItem += new DrawItemEventHandler(cmbProgramas_DrawItem);
        }

        private void txtTemporizador_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsDigit(e.KeyChar) || e.KeyChar == (char)Keys.Back) // Verifica se o que foi digitado pelo teclado é válido ou não.
            {
                e.Handled = false; // Realiza a inclusão do caracter.
            }
            else
            {
                e.Handled = true; // Trava a inclusão do caracter.
            }
        }

        private void txtTemporizador_TextChanged(object sender, EventArgs e)
        {
            if (modoPrograma == true) // Caso seja um programa de Aquecimento que esteja rodando, desabilitar o temporizador.
            {
                txtTemporizador.Enabled = false;
                return;
            }
        }

        private void cmbProgramas_DrawItem(object sender, DrawItemEventArgs e)
        {
            // Verifica se o índice do item é válido.
            if (e.Index < 0 || e.Index >= cmbProgramas.Items.Count)
            {
                return; // Se o índice for inválido, não faz nada.
            }

            // Desenha o fundo do item.
            e.DrawBackground();

            // Obtém o comboBox que está enviando o evento.
            ComboBox comboBox = (ComboBox)sender;

            // Obtém o item a ser desenhado no comboBox.
            var item = (KeyValuePair<string, ListaPrograma>)comboBox.Items[e.Index];

            // Verifica se o item é do banco de dados ou é um item fixo.
            bool isFromDatabase = !listaProgramaFixos.ContainsKey(item.Key);

            // Define a fonte em itálico se o item é do banco de dados, caso contrário, usa a fonte padrão.
            Font itemFont = isFromDatabase ? new Font(e.Font, FontStyle.Italic) : e.Font;

            // Define a cor do texto.
            Brush itemBrush = new SolidBrush(e.ForeColor);

            // Altera a fonte do texto no comboBox com a fonte e a cor.
            e.Graphics.DrawString(item.Key, itemFont, itemBrush, e.Bounds);

            // Desenha o retângulo de foco ao redor do item.
            e.DrawFocusRectangle();
        }

        private void CarregarProgramas()
        {
            // Inicializa o dicionário para armazenar os programas de aquecimento.
            listaPrograma = new Dictionary<string, ListaPrograma>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                // Define a consulta SQL para selecionar todos os programas de aquecimento da tabela.
                string query = "SELECT Nome_Programa, Potencia, Alimento, Tempo, char_Aquecimento, Instrucao FROM Programas";
                // Cria um comando SQL com a consulta e a conexão.
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    try
                    {
                        // Abre a conexão com o banco de dados.
                        connection.Open();
                        // Executa a consulta e obtém um leitor de dados.
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            // Itera através dos resultados da consulta.
                            while (reader.Read())
                            {
                                // Lê os dados da linha atual do leitor e os converte para os tipos apropriados.
                                string nomePrograma = reader["Nome_Programa"].ToString();
                                int potencia = Convert.ToInt32(reader["Potencia"]);
                                string alimento = reader["Alimento"]?.ToString();
                                int tempo = Convert.ToInt32(reader["Tempo"]);
                                char charAquecimento = Convert.ToChar(reader["char_Aquecimento"]);
                                string instrucoes = reader["Instrucao"].ToString();

                                // Adiciona os programa de aquecimento ao dicionário, usando o nome do programa de aquecimento como chave.
                                listaPrograma[nomePrograma] = new ListaPrograma
                                {
                                    Nome = nomePrograma,
                                    Comida = alimento,
                                    Tempo = tempo,
                                    Potencia = potencia,
                                    stringAquecimento = charAquecimento,
                                    Instrucoes = instrucoes
                                };
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Exibe uma mensagem de erro se ocorrer uma exceção ao carregar os dados.
                        MessageBox.Show("Erro ao carregar os programas: " + ex.Message);
                    }
                }
            }
        }

        private void AtualizarComboBox()
        {
            // Cria um dicionário para armazenar os programas de aquecimento carregados do banco de dados.
            Dictionary<string, ListaPrograma> programasDoBanco = new Dictionary<string, ListaPrograma>();

            try
            {
                // Cria uma conexão com o banco de dados.
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    // Define a consulta SQL para selecionar todos os programas de aquecimento.
                    string query = "SELECT Nome_Programa, Potencia, Tempo, char_Aquecimento, Instrucao FROM Programas";
                    // Cria um comando SQL com a consulta e a conexão.
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Executa a consulta e obtém um leitor de dados.
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            // Itera através dos resultados da consulta.
                            while (reader.Read())
                            {
                                // Lê e converte os dados da linha atual do leitor.
                                string nomePrograma = reader.GetString(0); // Nome do programa.
                                int potencia = reader.GetInt32(1); // Potência.
                                int tempo = reader.GetInt32(2); // Tempo.
                                char charAquecimento = reader.GetString(3)[0]; // Caracter de aquecimento.
                                string instrucao = reader.GetString(4); // Instrução.

                                // Adiciona o programa ao dicionário de programas de aquecimento do banco.
                                programasDoBanco[nomePrograma] = new ListaPrograma
                                {
                                    Nome = nomePrograma,
                                    Potencia = potencia,
                                    Tempo = tempo,
                                    stringAquecimento = charAquecimento,
                                    Instrucoes = instrucao
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Exibe uma mensagem de erro se ocorrer uma exceção ao carregar os dados.
                MessageBox.Show($"Erro ao carregar os programas: {ex.Message}");
            }

            // Cria um dicionário combinando os programas de aquecimento fixos com os programas carregados do banco.
            var todosProgramas = new Dictionary<string, ListaPrograma>(listaProgramaFixos);
            foreach (var programa in programasDoBanco)
            {
                // Adiciona apenas programas de aquecimento do banco que ainda não estão no dicionário de programas fixos.
                if (!todosProgramas.ContainsKey(programa.Key))
                {
                    todosProgramas.Add(programa.Key, programa.Value);
                }
            }

            // Atualiza o ComboBox com todos os programas de aquecimento combinados.
            cmbProgramas.DataSource = new BindingSource(todosProgramas, null);
        }

        private void trackBarPower_Scroll(object sender, EventArgs e)
        {
            //Altera o Label da potencia para ser mostrado a potencia selecionada.
            lblPotencia.Text = trackBarPower.Value.ToString();
        }

        private void Timer_Tick(object sender, EventArgs e) // Metodo Criado para o temporizador.
        {
            if (totalTime > 0) // Valida se o tempo total é maior que zero.
            {
                totalTime--; // Faz a diminuição por segundo.
                UpdateTimeDisplay(); //Atualiza o displey do temporizador.

                int numPot = trackBarPower.Value; // Cria uma variável que toma o valor selecionado na TrackBarPower.
                lblPotencia.Text = numPot.ToString(); // Label tomando o valor da Potencia selecionada.
                if (numPot > 1)
                {
                    if (modoPrograma == true) // Valida se o modo programa está selecionado, para que o char seja oque foi pré-definido e não o padrão.
                    {
                        // Irá ser utilizado o char que foi pré-definido no programa de Aquecimento.
                        lblAquecimento.Text += new string(caracterAquecimento, numPot);
                        lblAquecimento.Text += " "; // Apenas adiciona um espaço entre os ticks por segundo.
                    }
                    else
                    {
                        // Irá ser utilizado o char como padrão.
                        lblAquecimento.Text += new string('.', numPot);
                        lblAquecimento.Text += " "; // Apenas adiciona um espaço entre os ticks por segundo.
                    }
                }
            }
            else // Caso o tempo finalize, entre nessa condição.
            {
                timer.Stop(); // Para o Temporizador.
                lblAquecimento.Text += "Aquecimento Concluído"; // Informe na tela que o Aquecimento foi concluído.

                trackBarPower.Enabled = true; // Habilita a Barra da potência para ser alterada.
                cmbProgramas.Enabled = true; // Habilita o comboBox de Programas a ser selecionado.

                MessageBox.Show("Tempo esgotado!"); // Informa que o tempo Foi finalizado.
                tempoFinalizado++;
            }
        }

        private void UpdateTimeDisplay()
        {
            // Realiza a conversão do tempo selecionado para minutos e segundos.
            int minutes = totalTime / 60;
            int seconds = totalTime % 60;
            txtTemporizador.Text = $"{minutes:D2}:{seconds:D2}"; // Informa no temporizador o tempo convertido.
        }

        private void InicializaPrgramaAquecimentoFixos()
        {
            // Lista dos programas de aquecimento fixos.
            listaProgramaFixos = new Dictionary<string, ListaPrograma>
            {
                {
                    "Pipoca", new ListaPrograma // Programa de aquecimento da Pipoca.
                    {
                        Nome = "Pipoca",
                        Comida = "Pipoca (de Micro-ondas)",
                        Tempo = 180,
                        Potencia = 7,
                        stringAquecimento = '@',
                        Instrucoes = "Observar o barulho de estouros do milho, caso houver um intervalo de mais de 10 segundos entre um estouro e outro, interrompa o aquecimento."
                    }
                },
                {
                    "Leite", new ListaPrograma // Programa de aquecimento do Leite.
                    {
                        Nome = "Leite",
                        Comida = "Leite",
                        Tempo = 300,
                        Potencia = 5,
                        stringAquecimento = '#',
                        Instrucoes = "Cuidado com aquecimento de líquidos, o choque térmico aliado ao movimento do recipiente pode causar fervura imediata causando risco de queimaduras."
                    }
                },
                {
                    "Carnes de boi", new ListaPrograma // Programa de aquecimento da Carne de boi.
                    {
                        Nome = "Carnes de boi",
                        Comida = "Carne em pedaços ou fatias",
                        Tempo = 840,
                        Potencia = 4,
                        stringAquecimento = '$',
                        Instrucoes = "Interrompa o processo na metade e vire o conteúdo com a parte de baixo para cima para o descongelamento uniforme."
                    }
                },
                {
                    "Frango", new ListaPrograma // Programa de aquecimento do Frango.
                    {
                        Nome = "Frango",
                        Comida = "Frango (qualquer corte)",
                        Tempo = 480,
                        Potencia = 7,
                        stringAquecimento = '%',
                        Instrucoes = "Interrompa o processo na metade e vire o conteúdo com a parte de baixo para cima para o descongelamento uniforme."
                    }
                },
                {
                    "Feijão", new ListaPrograma // Programa de aquecimento do Feijão.
                    {
                        Nome = "Feijão",
                        Comida = "Feijão congelado",
                        Tempo = 480,
                        Potencia = 9,
                        stringAquecimento = '&',
                        Instrucoes = "Deixe o recipiente destampado e em casos de plástico, cuidado ao retirar o recipiente pois o mesmo pode perder resistência em altas temperaturas."
                    }
                }
            };
        }

        private void cmbProgramas_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (inicializandoComboBox) // Confirma o inicializador do comboBox.
            {
                return;
            }

            if (cmbProgramas.SelectedItem is KeyValuePair<string, ListaPrograma> programaSelecionado) // Valida o tipo do item selecionado, para obter as informações de cada programa de aquecimento.
            {
                ListaPrograma programa = programaSelecionado.Value;

                // Configura o tempo e a potência do programa de aquecimento selecionado.
                totalTime = programa.Tempo;
                trackBarPower.Value = programa.Potencia;
                lblPotencia.Text = programa.Potencia.ToString();
                caracterAquecimento = programa.stringAquecimento;

                // Limpa o campo de temporizador e define o modo de programa de aquecimento.
                txtTemporizador.Text = string.Empty;
                modoPrograma = true;

                UpdateTimeDisplay(); // Atualiza o temporizador.

                // Exibe a mensagem com as instruções do programa selecionado
                MessageBox.Show($"Programa: {programa.Nome}\nAlimento: {programa.Comida}\nInstruções: {programa.Instrucoes}");
            }
        }
    }
}
